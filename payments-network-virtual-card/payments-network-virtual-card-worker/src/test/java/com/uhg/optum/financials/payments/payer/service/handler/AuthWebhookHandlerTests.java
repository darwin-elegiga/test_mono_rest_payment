package com.uhg.optum.financials.payments.payer.service.handler;

import com.uhg.optum.financials.payments.payer.constant.EventTypeConstants;
import com.uhg.optum.financials.payments.payer.dto.authevent.AuthorizationPayload;
import com.uhg.optum.financials.payments.payer.dto.authevent.MerchantInfo;
import com.uhg.optum.financials.payments.payer.dto.authevent.Money;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.OrphanEvents;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.OrphanEventsRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.service.impl.VirtualCardBalanceService;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.ModelConversionMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.Mockito;

import java.util.Optional;
import java.util.UUID;

public class AuthWebhookHandlerTests {

	private LogUtility logUtility;

	private AuditLogger auditLogger;

	private VirtualCardsRepository virtualCardsRepository;

	private VirtualCardBalanceService virtualCardBalanceService;

	private AuthExtractRepository authExtractRepository;

	private WebhookClient webhookClient;

	private ModelConversionMapper cardDetailsMapper;

	private OrphanEventsRepository orphanEventsRepository;

	private PaymentRequestRepository paymentRequestRepository;

	private AuthWebhookHandler authWebhookHandler;

	@BeforeEach
	void setUp() {
		this.logUtility = Mockito.mock(LogUtility.class);
		this.auditLogger = Mockito.mock(AuditLogger.class);
		this.virtualCardsRepository = Mockito.mock(VirtualCardsRepository.class);
		this.virtualCardBalanceService = Mockito.mock(VirtualCardBalanceService.class);
		this.authExtractRepository = Mockito.mock(AuthExtractRepository.class);
		this.webhookClient = Mockito.mock(WebhookClient.class);
		this.cardDetailsMapper = Mockito.mock(ModelConversionMapper.class);
		this.orphanEventsRepository = Mockito.mock(OrphanEventsRepository.class);
		this.paymentRequestRepository = Mockito.mock(PaymentRequestRepository.class);
		this.authWebhookHandler = new AuthWebhookHandler(this.logUtility, this.auditLogger, this.virtualCardsRepository,
				this.virtualCardBalanceService, this.authExtractRepository, this.webhookClient, this.cardDetailsMapper,
				this.orphanEventsRepository, this.paymentRequestRepository);
	}

	@Test
	void testProcess_approvedEvent_virtualCardFound() throws Exception {
		AuthorizationPayload payload = this.createPayload(EventTypeConstants.AUTH_EVENT_APPROVED);
		VirtualCard virtualCard = Mockito.mock(VirtualCard.class);
		Mockito.when(this.virtualCardsRepository.findByCardGuid(UUID.fromString(payload.getCardId())))
			.thenReturn(Optional.of(virtualCard));
		AuthExtract authExtract = new AuthExtract();
		Mockito.when(this.cardDetailsMapper.modelMapper(payload, AuthExtract.class)).thenReturn(authExtract);
		Mockito.when(virtualCard.getCardGuid()).thenReturn(UUID.fromString(payload.getCardId()));
		Mockito.when(virtualCard.getPaymentId()).thenReturn("paymentId");
		Mockito.doNothing()
			.when(this.virtualCardBalanceService)
			.updateBalanceForAuthorization(Mockito.any(), Mockito.any(), Mockito.anyString(), Mockito.isNull());
		Mockito.doNothing().when(this.auditLogger).logAuthAccepted(Mockito.any(), Mockito.any(), Mockito.any());
		Mockito.when(this.authExtractRepository.save(Mockito.any())).thenReturn(authExtract);
		Mockito.doNothing().when(this.logUtility).logMessage(Mockito.anyString(), Mockito.anyString(), Mockito.any());
		PaymentRequest mockPaymentRequest = Mockito.mock(PaymentRequest.class);
		Mockito.when(this.paymentRequestRepository.findByPaymentId(Mockito.anyString())).thenReturn(mockPaymentRequest);
		Mockito.doNothing()
			.when(this.auditLogger)
			.logException(Mockito.any(), Mockito.any(), Mockito.any(), Mockito.any(), Mockito.any(), Mockito.any());
		this.authWebhookHandler.process(payload);
		Mockito.verify(this.virtualCardBalanceService)
			.updateBalanceForAuthorization(Mockito.any(), Mockito.any(), Mockito.anyString(), Mockito.isNull());
		Mockito.verify(this.auditLogger).logAuthAccepted(Mockito.any(), Mockito.any(), Mockito.any());
		Mockito.verify(this.authExtractRepository).save(Mockito.any());
		Mockito.verify(this.logUtility, Mockito.atLeastOnce())
			.logMessage(Mockito.contains("approved event saved"), Mockito.anyString(), Mockito.any());
	}

	@Test
	void testProcess_declinedEvent_virtualCardFound() throws Exception {
		AuthorizationPayload payload = this.createPayload(EventTypeConstants.AUTH_EVENT_DECLINED);
		VirtualCard virtualCard = Mockito.mock(VirtualCard.class);
		Mockito.when(this.virtualCardsRepository.findByCardGuid(UUID.fromString(payload.getCardId())))
			.thenReturn(Optional.of(virtualCard));
		AuthExtract authExtract = new AuthExtract();
		Mockito.when(this.cardDetailsMapper.modelMapper(payload, AuthExtract.class)).thenReturn(authExtract);
		Mockito.when(virtualCard.getCardGuid()).thenReturn(UUID.fromString(payload.getCardId()));
		Mockito.when(virtualCard.getPaymentId()).thenReturn("paymentId");
		Mockito.doNothing().when(this.auditLogger).logAuthRejected(Mockito.any(), Mockito.any(), Mockito.any());
		Mockito.when(this.authExtractRepository.save(Mockito.any())).thenReturn(authExtract);
		Mockito.doNothing().when(this.logUtility).logMessage(Mockito.anyString(), Mockito.anyString(), Mockito.any());
		PaymentRequest mockPaymentRequest = Mockito.mock(PaymentRequest.class);
		Mockito.when(this.paymentRequestRepository.findByPaymentId(Mockito.anyString())).thenReturn(mockPaymentRequest);
		this.authWebhookHandler.process(payload);
		Mockito.verify(this.auditLogger).logAuthRejected(Mockito.any(), Mockito.any(), Mockito.any());
		Mockito.verify(this.authExtractRepository).save(Mockito.any());
		Mockito.verify(this.logUtility, Mockito.atLeastOnce())
			.logMessage(Mockito.contains("declined event saved"), Mockito.anyString(), Mockito.any());
	}

	@Test
	void testProcess_virtualCardNotFound_savesOrphanEvent() throws Exception {
		AuthorizationPayload payload = this.createPayload(EventTypeConstants.AUTH_EVENT_APPROVED);
		Mockito.when(this.virtualCardsRepository.findByCardGuid(UUID.fromString(payload.getCardId())))
			.thenReturn(Optional.empty());
		Mockito.doNothing().when(this.logUtility).logMessage(Mockito.anyString(), Mockito.anyString(), Mockito.any());
		Mockito.when(this.orphanEventsRepository.save(Mockito.any())).thenReturn(new OrphanEvents());
		this.authWebhookHandler.process(payload);
		Mockito.verify(this.orphanEventsRepository).save(Mockito.any());
		Mockito.verify(this.logUtility)
			.logMessage(Mockito.contains("VirtualCard not found"), Mockito.anyString(), Mockito.any());
	}

	@Test
	void testProcess_exceptionInApprovedFlow_logsException() throws Exception {
		AuthorizationPayload payload = this.createPayload(EventTypeConstants.AUTH_EVENT_APPROVED);
		VirtualCard virtualCard = Mockito.mock(VirtualCard.class);
		Mockito.when(this.virtualCardsRepository.findByCardGuid(UUID.fromString(payload.getCardId())))
			.thenReturn(Optional.of(virtualCard));
		Mockito.when(this.cardDetailsMapper.modelMapper(payload, AuthExtract.class))
			.thenThrow(new RuntimeException("fail"));
		Mockito.doNothing()
			.when(this.auditLogger)
			.logException(Mockito.any(), Mockito.any(), Mockito.any(), Mockito.any(), Mockito.any(), Mockito.any());
		Mockito.doNothing().when(this.logUtility).logMessage(Mockito.anyString(), Mockito.anyString(), Mockito.any());
		this.authWebhookHandler.process(payload);
		Mockito.verify(this.auditLogger)
			.logException(Mockito.any(), Mockito.any(), Mockito.any(), Mockito.any(), Mockito.any(), Mockito.any());
		Mockito.verify(this.logUtility)
			.logMessage(Mockito.contains("Error while processing auth event"), Mockito.anyString(), Mockito.any());
	}

	private AuthorizationPayload createPayload(String status) {
		AuthorizationPayload payload = new AuthorizationPayload();
		payload.setCardId(UUID.randomUUID().toString());
		payload.setTransactionStatus(status);
		Money money = new Money();
		money.setValue("1000");
		money.setCurrency("USD");
		payload.setAuthAmount(money);
		payload.setOriginalAuthRequestId("originalAuthRequestId");
		payload.setReceiptId(UUID.randomUUID().toString());
		payload.setMerchantInfo(new MerchantInfo());
		payload.setSystemAuthTime(null);
		payload.setAuthExpiryTime(null);
		return payload;
	}

}
