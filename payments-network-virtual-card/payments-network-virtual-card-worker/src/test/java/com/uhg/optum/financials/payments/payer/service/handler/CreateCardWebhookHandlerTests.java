package com.uhg.optum.financials.payments.payer.service.handler;

import com.uhg.optum.financials.payments.payer.dto.BinRange;
import com.uhg.optum.financials.payments.payer.dto.CardDetailsDTO;
import com.uhg.optum.financials.payments.payer.dto.SensitiveValue;
import com.uhg.optum.financials.payments.payer.dto.SensitiveView;
import com.uhg.optum.financials.payments.payer.dto.callback.CreateCardData;
import com.uhg.optum.financials.payments.payer.dto.webhook.WebhookBaseDto;
import com.uhg.optum.financials.payments.payer.enums.CardOperationStatus;
import com.uhg.optum.financials.payments.payer.enums.CompletionStage;
import com.uhg.optum.financials.payments.payer.enums.Status;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.ReprocessLockRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardHistoryRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.ConsentDecisionService;
import com.uhg.optum.financials.payments.payer.service.EncryptionAtRestService;
import com.uhg.optum.financials.payments.payer.service.SummaryPageService;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.service.StpRemittanceService;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.ModelConversionMapper;
import com.uhg.optum.financials.payments.payer.util.PaymentRequestDtoUtil;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InOrder;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import java.math.BigDecimal;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
public class CreateCardWebhookHandlerTests {

	@InjectMocks
	CreateCardWebhookHandler createCardWebhookHandler;

	@Mock
	VirtualCardsRepository virtualCardsRepository;

	@Mock
	PaymentRequestRepository paymentRequestRepository;

	@Mock
	VirtualCardHistoryRepository virtualCardHistoryRepository;

	@Mock
	CardCreationRepository cardCreationRepository;

	@Mock
	ZetaApiService zetaApiService;

	@Mock
	EncryptionAtRestService encryptionAtRestService;

	@Mock
	ModelConversionMapper cardDetailsMapper;

	@Mock
	SummaryPageService summaryPageService;

	@Mock
	StpRemittanceService stpRemittanceService;

	@Mock
	AuditLogger auditLogger;

	@Mock
	WebhookClient webhookClient;

	@Mock
	VirtualCardUtil virtualCardUtil;

	@Mock
	LogUtility logUtility;

	@Mock
	ConsentDecisionService consentDecisionService;

	@Mock
	ReprocessLockRepository reprocessLockRepository;

	@Mock
	PaymentRequestDtoUtil paymentRequestDtoUtil;

	// @Test
	public void testHandleCreate() throws Exception {
		CardCreation cardCreation = new CardCreation();
		BDDMockito.when(this.virtualCardUtil.getCardCreationByRequestId(any())).thenReturn(cardCreation);
		CreateCardData createCardData = new CreateCardData();
		CreateCardData.InitialBalance availableBalance = new CreateCardData.InitialBalance();
		availableBalance.setAmount(100L);
		createCardData.setInitialBalance(availableBalance);
		createCardData.setCardGuid(UUID.randomUUID());
		createCardData.setClientRequestId(UUID.fromString("f1a8f5fa-87ab-4060-918b-9289a23d4e35"));
		createCardData.setAutoActivateCard(Boolean.FALSE);
		createCardData.setSingleSpend(Boolean.FALSE);
		PaymentRequest paymentRequest = new PaymentRequest();
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("f1a8f5fa-87eb-4060-918b-9289a23d4e35");
		paymentDetails.setAmount(BigDecimal.valueOf(100));
		paymentRequest.setPaymentDetails(paymentDetails);
		BDDMockito.when(this.virtualCardUtil.updatePaymentRequestStatus(any(), any(), any()))
			.thenReturn(paymentRequest);
		CardDetailsDTO mockCardDetailsDTO = new CardDetailsDTO();
		BinRange binRange = new BinRange();
		binRange.setBin("464646");
		mockCardDetailsDTO.setBinRange(binRange);
		SensitiveView sensitiveView = new SensitiveView();
		SensitiveValue value = new SensitiveValue();
		value.setEncryptedData("encryptedData");
		sensitiveView.setValue(value);

		BDDMockito.when(this.zetaApiService.getCardDetails(any())).thenReturn(mockCardDetailsDTO);
		BDDMockito.when(this.paymentRequestRepository.findByPaymentId(any())).thenReturn(paymentRequest);
		CardDetailsDTO cardDetailsDTO = new CardDetailsDTO();
		cardDetailsDTO.setMaskedPan("4111111111111111");
		BDDMockito.when(this.zetaApiService.getCardDetails(any())).thenReturn(cardDetailsDTO);

		VirtualCard virtualCard = new VirtualCard();
		Mockito.when(this.virtualCardsRepository.save(any())).thenReturn(virtualCard);
		this.createCardWebhookHandler.process(createCardData);
		Mockito.verify(this.virtualCardsRepository, Mockito.times(1)).save(any());
	}

	// @Test
	public void testHandleCreate_ExceptionLoggingAndStatusUpdate() throws Exception {
		CreateCardData createCardData = new CreateCardData();
		createCardData.setClientRequestId(UUID.randomUUID());
		createCardData.setCardGuid(UUID.randomUUID());

		CardCreation cardCreation = new CardCreation();
		cardCreation.setPaymentId("payment-id-123");
		BDDMockito.when(this.virtualCardUtil.getCardCreationByRequestId(any())).thenReturn(cardCreation);
		BDDMockito.when(this.virtualCardUtil.updatePaymentRequestStatus(any(), any(), any()))
			.thenReturn(new PaymentRequest());

		this.createCardWebhookHandler.process(createCardData);

		Mockito.verify(this.logUtility)
			.logMessage(
					Mockito.contains("Error while fetching card details for cardGuid: " + createCardData.getCardGuid()),
					Mockito.eq(ERROR), Mockito.any());
		Mockito.verify(this.logUtility)
			.logMessageWithPriority(
					Mockito
						.contains("Virtual card creation failed for requestId: " + createCardData.getClientRequestId()),
					Mockito.eq(ERROR), Mockito.any(), Mockito.eq(P3));
	}

	@Test
	void testProcess_ResumesFromDocumentGenerated_SkipsAlreadyCompletedSteps() throws Exception {
		UUID requestId = UUID.randomUUID();
		UUID cardGuid = UUID.randomUUID();

		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(requestId);
		cardCreation.setPaymentId("payment-id-resume");
		cardCreation.setStatus(CardOperationStatus.CARD_ISSUED.name());
		cardCreation.setCompletionStage(CompletionStage.DOCUMENT_GENERATED.name());
		cardCreation.setBin(464646L);
		BDDMockito.when(this.virtualCardUtil.getCardCreationByRequestId(any())).thenReturn(cardCreation);

		PaymentRequest paymentRequest = new PaymentRequest();
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("payment-id-resume");
		paymentDetails.setAmount(BigDecimal.valueOf(100));
		paymentDetails.setNetworkPaymentId("NP123");
		paymentRequest.setPaymentDetails(paymentDetails);
		BDDMockito.when(this.paymentRequestRepository.findByPaymentId(any())).thenReturn(paymentRequest);

		VirtualCard existingVirtualCard = new VirtualCard();
		existingVirtualCard.setCardGuid(cardGuid);
		existingVirtualCard.setMaskedPan("****1111");
		BDDMockito.when(this.virtualCardsRepository.findByPaymentId(any())).thenReturn(existingVirtualCard);

		CreateCardData createCardData = new CreateCardData();
		createCardData.setClientRequestId(requestId);
		createCardData.setCardGuid(cardGuid);

		this.createCardWebhookHandler.process(createCardData);

		// Steps already recorded as done on a prior run must not be repeated.
		// (completion webhook now sends BEFORE document generation, so reaching
		// DOCUMENT_GENERATED means it was already sent on an earlier run.)
		Mockito.verify(this.zetaApiService, Mockito.never()).getCardDetails(any());
		Mockito.verify(this.virtualCardsRepository, Mockito.never()).save(any());
		Mockito.verify(this.summaryPageService, Mockito.never()).processSummaryDocumentMessage(any(), any(), any());
		Mockito.verify(this.webhookClient, Mockito.never())
			.notifyClientWithStatus(any(WebhookBaseDto.class), any(), any());

		// The one step not yet done must still run so the payment actually finishes.
		Mockito.verify(this.stpRemittanceService, Mockito.times(1)).sendRemittance(any());

		// Lock held for the whole call must be released once processing finishes.
		Mockito.verify(this.reprocessLockRepository, Mockito.times(1)).releaseLock("payment:payment-id-resume");
	}

	@Test
	void testProcess_ResumesFromVirtualCardCreated_SendsWebhookBeforeDocumentAndStp() throws Exception {
		UUID requestId = UUID.randomUUID();
		UUID cardGuid = UUID.randomUUID();

		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(requestId);
		cardCreation.setPaymentId("payment-id-resume-2");
		cardCreation.setStatus(CardOperationStatus.CARD_ISSUED.name());
		cardCreation.setCompletionStage(CompletionStage.VIRTUAL_CARD_CREATED.name());
		cardCreation.setBin(464646L);
		BDDMockito.when(this.virtualCardUtil.getCardCreationByRequestId(any())).thenReturn(cardCreation);

		PaymentRequest paymentRequest = new PaymentRequest();
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("payment-id-resume-2");
		paymentDetails.setAmount(BigDecimal.valueOf(100));
		paymentDetails.setNetworkPaymentId("NP456");
		paymentRequest.setPaymentDetails(paymentDetails);
		BDDMockito.when(this.paymentRequestRepository.findByPaymentId(any())).thenReturn(paymentRequest);

		VirtualCard existingVirtualCard = new VirtualCard();
		existingVirtualCard.setCardGuid(cardGuid);
		existingVirtualCard.setMaskedPan("****2222");
		BDDMockito.when(this.virtualCardsRepository.findByPaymentId(any())).thenReturn(existingVirtualCard);

		BDDMockito.when(this.webhookClient.notifyClientWithStatus(any(WebhookBaseDto.class), any(), any()))
			.thenReturn(true);

		CreateCardData createCardData = new CreateCardData();
		createCardData.setClientRequestId(requestId);
		createCardData.setCardGuid(cardGuid);

		this.createCardWebhookHandler.process(createCardData);

		// VirtualCard already exists — must not touch Zeta/insert again.
		Mockito.verify(this.zetaApiService, Mockito.never()).getCardDetails(any());
		Mockito.verify(this.virtualCardsRepository, Mockito.never()).save(any());

		// Nothing past VIRTUAL_CARD_CREATED has run yet — webhook, document, and STP
		// must all execute, with webhook strictly before document generation.
		InOrder order = Mockito.inOrder(this.webhookClient, this.summaryPageService, this.stpRemittanceService);
		order.verify(this.webhookClient).notifyClientWithStatus(any(WebhookBaseDto.class), any(), any());
		order.verify(this.summaryPageService).processSummaryDocumentMessage(any(), any(), any());
		order.verify(this.stpRemittanceService).sendRemittance(any());

		// Lock held for the whole call must be released once processing finishes.
		Mockito.verify(this.reprocessLockRepository, Mockito.times(1)).releaseLock("payment:payment-id-resume-2");
	}

	@Test
	void testProcess_WebhookDeliveryFails_DoesNotAdvanceStageOrRunLaterSteps() throws Exception {
		UUID requestId = UUID.randomUUID();
		UUID cardGuid = UUID.randomUUID();

		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(requestId);
		cardCreation.setPaymentId("payment-id-webhook-fail");
		cardCreation.setStatus(CardOperationStatus.CARD_ISSUED.name());
		cardCreation.setCompletionStage(CompletionStage.VIRTUAL_CARD_CREATED.name());
		cardCreation.setBin(464646L);
		BDDMockito.when(this.virtualCardUtil.getCardCreationByRequestId(any())).thenReturn(cardCreation);

		PaymentRequest paymentRequest = new PaymentRequest();
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("payment-id-webhook-fail");
		paymentDetails.setAmount(BigDecimal.valueOf(100));
		paymentDetails.setNetworkPaymentId("NP789");
		paymentRequest.setPaymentDetails(paymentDetails);
		BDDMockito.when(this.paymentRequestRepository.findByPaymentId(any())).thenReturn(paymentRequest);

		VirtualCard existingVirtualCard = new VirtualCard();
		existingVirtualCard.setCardGuid(cardGuid);
		existingVirtualCard.setMaskedPan("****3333");
		BDDMockito.when(this.virtualCardsRepository.findByPaymentId(any())).thenReturn(existingVirtualCard);

		BDDMockito.when(this.webhookClient.notifyClientWithStatus(any(WebhookBaseDto.class), any(), any()))
			.thenReturn(false);

		CreateCardData createCardData = new CreateCardData();
		createCardData.setClientRequestId(requestId);
		createCardData.setCardGuid(cardGuid);

		this.createCardWebhookHandler.process(createCardData);

		// Delivery failed — completion_stage must stay behind COMPLETION_WEBHOOK_SENT so
		// a later reprocess attempt retries sending it, and nothing after it should run.
		Assertions.assertThat(cardCreation.getCompletionStage())
			.isNotEqualTo(CompletionStage.COMPLETION_WEBHOOK_SENT.name());
		Mockito.verify(this.summaryPageService, Mockito.never()).processSummaryDocumentMessage(any(), any(), any());
		Mockito.verify(this.stpRemittanceService, Mockito.never()).sendRemittance(any());

		// Lock must be released even when processing fails partway through — otherwise a
		// failed delivery would permanently block any further reprocess attempt.
		Mockito.verify(this.reprocessLockRepository, Mockito.times(1)).releaseLock("payment:payment-id-webhook-fail");
	}

	@Test
	void testProcess_DocumentGenerationFails_DoesNotAdvanceStageOrRunStp() throws Exception {
		UUID requestId = UUID.randomUUID();
		UUID cardGuid = UUID.randomUUID();

		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(requestId);
		cardCreation.setPaymentId("payment-id-doc-fail");
		cardCreation.setStatus(CardOperationStatus.CARD_ISSUED.name());
		cardCreation.setCompletionStage(CompletionStage.COMPLETION_WEBHOOK_SENT.name());
		cardCreation.setBin(464646L);
		BDDMockito.when(this.virtualCardUtil.getCardCreationByRequestId(any())).thenReturn(cardCreation);

		PaymentRequest paymentRequest = new PaymentRequest();
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("payment-id-doc-fail");
		paymentDetails.setAmount(BigDecimal.valueOf(100));
		paymentDetails.setNetworkPaymentId("NP999");
		paymentRequest.setPaymentDetails(paymentDetails);
		BDDMockito.when(this.paymentRequestRepository.findByPaymentId(any())).thenReturn(paymentRequest);

		VirtualCard existingVirtualCard = new VirtualCard();
		existingVirtualCard.setCardGuid(cardGuid);
		existingVirtualCard.setMaskedPan("****4444");
		BDDMockito.when(this.virtualCardsRepository.findByPaymentId(any())).thenReturn(existingVirtualCard);

		Mockito.doThrow(new RuntimeException("document service unavailable"))
			.when(this.summaryPageService)
			.processSummaryDocumentMessage(any(), any(), any());

		CreateCardData createCardData = new CreateCardData();
		createCardData.setClientRequestId(requestId);
		createCardData.setCardGuid(cardGuid);

		this.createCardWebhookHandler.process(createCardData);

		// Generation failed — completion_stage must stay behind DOCUMENT_GENERATED so a
		// later reprocess attempt retries it, and STP must not run in the meantime.
		Assertions.assertThat(cardCreation.getCompletionStage())
			.isNotEqualTo(CompletionStage.DOCUMENT_GENERATED.name());
		Mockito.verify(this.stpRemittanceService, Mockito.never()).sendRemittance(any());

		// Lock must be released even when processing fails partway through — otherwise a
		// failed generation would permanently block any further reprocess attempt.
		Mockito.verify(this.reprocessLockRepository, Mockito.times(1)).releaseLock("payment:payment-id-doc-fail");
	}

	@Test
	void testProcess_ReprocessCompletesAllStages_RestoresPaymentRequestToPI() throws Exception {
		UUID requestId = UUID.randomUUID();
		UUID cardGuid = UUID.randomUUID();

		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(requestId);
		cardCreation.setPaymentId("payment-id-reprocess-success");
		cardCreation.setStatus(CardOperationStatus.CARD_ISSUED.name());
		cardCreation.setCompletionStage(CompletionStage.VIRTUAL_CARD_CREATED.name());
		cardCreation.setBin(464646L);
		BDDMockito.when(this.virtualCardUtil.getCardCreationByRequestId(any())).thenReturn(cardCreation);

		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setStatus(Status.PE.name());
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("payment-id-reprocess-success");
		paymentDetails.setAmount(BigDecimal.valueOf(100));
		paymentDetails.setNetworkPaymentId("NP555");
		paymentRequest.setPaymentDetails(paymentDetails);
		BDDMockito.when(this.paymentRequestRepository.findByPaymentId(any())).thenReturn(paymentRequest);

		VirtualCard existingVirtualCard = new VirtualCard();
		existingVirtualCard.setCardGuid(cardGuid);
		existingVirtualCard.setMaskedPan("****5555");
		BDDMockito.when(this.virtualCardsRepository.findByPaymentId(any())).thenReturn(existingVirtualCard);

		BDDMockito.when(this.webhookClient.notifyClientWithStatus(any(WebhookBaseDto.class), any(), any()))
			.thenReturn(true);

		CreateCardData createCardData = new CreateCardData();
		createCardData.setClientRequestId(requestId);
		createCardData.setCardGuid(cardGuid);

		this.createCardWebhookHandler.process(createCardData);

		// A reprocessed payment that completes every stage must rejoin the normal flow
		// (PI), not stay stuck in PE, so the downstream settlement batch job picks it up.
		// Routed through virtualCardUtil so the transition also lands in
		// payment_request_history, same as every other status change in this handler,
		// with a message confirming the recovery rather than leaving only the original
		// failure reason on record.
		Mockito.verify(this.virtualCardUtil, Mockito.times(1))
			.updatePaymentRequestStatus(Mockito.eq("payment-id-reprocess-success"), Mockito.eq(Status.PI),
					Mockito.contains("successfully reprocessed"));

		// Every stage having run means the card creation is now fully done.
		Assertions.assertThat(cardCreation.getCompletionStage()).isEqualTo(CompletionStage.COMPLETED.name());

		// Lock held for the whole call must be released once processing finishes.
		Mockito.verify(this.reprocessLockRepository, Mockito.times(1))
			.releaseLock("payment:payment-id-reprocess-success");
	}

	@Test
	void testProcess_AlreadyCompleted_SkipsAllWorkButStillReleasesLock() throws Exception {
		UUID requestId = UUID.randomUUID();
		UUID cardGuid = UUID.randomUUID();

		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(requestId);
		cardCreation.setPaymentId("payment-id-already-completed");
		cardCreation.setStatus(CardOperationStatus.CARD_ISSUED.name());
		cardCreation.setCompletionStage(CompletionStage.COMPLETED.name());
		cardCreation.setBin(464646L);
		BDDMockito.when(this.virtualCardUtil.getCardCreationByRequestId(any())).thenReturn(cardCreation);

		CreateCardData createCardData = new CreateCardData();
		createCardData.setClientRequestId(requestId);
		createCardData.setCardGuid(cardGuid);

		this.createCardWebhookHandler.process(createCardData);

		// A redelivered/duplicate webhook for an already-COMPLETED payment must not
		// repeat
		// any work.
		Mockito.verify(this.zetaApiService, Mockito.never()).getCardDetails(any());
		Mockito.verify(this.virtualCardsRepository, Mockito.never()).save(any());
		Mockito.verify(this.summaryPageService, Mockito.never()).processSummaryDocumentMessage(any(), any(), any());
		Mockito.verify(this.webhookClient, Mockito.never())
			.notifyClientWithStatus(any(WebhookBaseDto.class), any(), any());
		Mockito.verify(this.stpRemittanceService, Mockito.never()).sendRemittance(any());
		Mockito.verify(this.cardCreationRepository, Mockito.never()).save(any());

		// The lock is acquired for the whole duration of processing, so it must still be
		// released on this early-return path too.
		Mockito.verify(this.reprocessLockRepository, Mockito.times(1))
			.releaseLock("payment:payment-id-already-completed");
	}

}
