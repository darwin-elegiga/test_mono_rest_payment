package com.uhg.optum.financials.payments.payer.service.handler;

import com.uhg.optum.financials.payments.payer.dto.callback.CallbackError;
import com.uhg.optum.financials.payments.payer.dto.webhook.WebhookBaseDto;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.CardCancellation;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.CardReset;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.CardCancellationRepository;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.CardResetRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Optional;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.EventTypeConstants.VIRTUAL_CARD_CANCEL;
import static com.uhg.optum.financials.payments.payer.constant.EventTypeConstants.VIRTUAL_CARD_CREATE;
import static com.uhg.optum.financials.payments.payer.constant.EventTypeConstants.VIRTUAL_CARD_RESET;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
public class WebhookCallbackErrorHandlerTests {

	@InjectMocks
	WebhookCallbackErrorHandler webhookCallbackErrorHandler;

	@Mock
	AuditLogger auditLogger;

	@Mock
	AuthExtractRepository authExtractRepository;

	@Mock
	LogUtility logUtility;

	@Mock
	VirtualCardsRepository virtualCardsRepository;

	@Mock
	CardCreationRepository cardCreationRepository;

	@Mock
	PaymentDetailsRepository paymentDetailsRepository;

	@Mock
	PaymentRequestRepository paymentRequestRepository;

	@Mock
	WebhookClient webhookClient;

	@Mock
	CardCancellationRepository cardCancellationRepository;

	@Mock
	CardResetRepository cardResetRepository;

	@Mock
	VirtualCardUtil virtualCardUtil;

	@Test
	public void testHandleCallbackError() {
		CardCreation cardCreation = new CardCreation();
		cardCreation.setPaymentId("12345678");
		BDDMockito.given(this.virtualCardUtil.getCardCreationByRequestId(any())).willReturn(cardCreation);
		CallbackError callbackError = new CallbackError();
		callbackError.setCode("500");
		callbackError.setMessage("Internal Server Error");
		callbackError.setClientRequestId(UUID.fromString("f1a8f5fa-87eb-4060-918b-9289a23d4e35"));
		PaymentDetails paymentDetails = new PaymentDetails();
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setId(Long.valueOf("12345678"));
		paymentRequest.setPaymentDetails(paymentDetails);
		paymentDetails.setId("f1a8f5fa-87eb-4060-918b-9289a23d4e35");
		BDDMockito.given(this.virtualCardUtil.updatePaymentRequestStatus(any(), any(), any()))
			.willReturn(paymentRequest);
		this.webhookCallbackErrorHandler.process(callbackError, VIRTUAL_CARD_CREATE);
		Mockito.verify(this.webhookClient, Mockito.times(1)).notifyClient(Mockito.<WebhookBaseDto>any(), any(), any());
	}

	@Test
	public void testHandleCallbackErrorCreateMissingPaymentDetails() {
		CardCreation cardCreation = new CardCreation();
		cardCreation.setPaymentId("12345678");
		BDDMockito.given(this.virtualCardUtil.getCardCreationByRequestId(any())).willReturn(cardCreation);
		CallbackError callbackError = new CallbackError();
		callbackError.setCode("500");
		callbackError.setMessage("Internal Server Error");
		callbackError.setClientRequestId(UUID.randomUUID());
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(null);
		BDDMockito.given(this.virtualCardUtil.updatePaymentRequestStatus(any(), any(), any()))
			.willReturn(paymentRequest);
		assertThatThrownBy(() -> this.webhookCallbackErrorHandler.process(callbackError, VIRTUAL_CARD_CREATE))
			.isInstanceOf(IllegalArgumentException.class);
	}

	@Test
	public void testHandleCallbackErrorCancel() {
		CardCancellation cardCancellation = new CardCancellation();
		cardCancellation.setCardGuid(UUID.randomUUID());
		cardCancellation.setPaymentId("payment-id-123");
		BDDMockito.given(this.cardCancellationRepository.findById(any())).willReturn(Optional.of(cardCancellation));
		CallbackError callbackError = new CallbackError();
		callbackError.setCode("500");
		callbackError.setMessage("Internal Server Error");
		callbackError.setClientRequestId(UUID.fromString("f1a8f5fa-87eb-4060-918b-9289a23d4e35"));
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("payment-id-123");
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(paymentDetails);
		BDDMockito.given(this.paymentDetailsRepository.findById((String) any()))
			.willReturn(Optional.of(paymentDetails));
		BDDMockito.given(this.paymentRequestRepository.findByPaymentId(any())).willReturn(paymentRequest);
		this.webhookCallbackErrorHandler.process(callbackError, VIRTUAL_CARD_CANCEL);
		Mockito.verify(this.webhookClient, Mockito.times(1)).notifyClient(Mockito.<WebhookBaseDto>any(), any(), any());
	}

	@Test
	public void testHandleCallbackErrorCancelNotFound() {
		BDDMockito.given(this.cardCancellationRepository.findById(any())).willReturn(Optional.empty());
		CallbackError callbackError = new CallbackError();
		callbackError.setCode("500");
		callbackError.setMessage("Internal Server Error");
		callbackError.setClientRequestId(UUID.randomUUID());
		assertThatThrownBy(() -> this.webhookCallbackErrorHandler.process(callbackError, VIRTUAL_CARD_CANCEL))
			.isInstanceOf(IllegalArgumentException.class);
	}

	@Test
	public void testHandleCallbackErrorReset() {
		CardReset cardReset = new CardReset();
		cardReset.setCardGuid(UUID.randomUUID());
		cardReset.setReceiptId("123456");
		BDDMockito.given(this.cardResetRepository.findByReceiptId(any())).willReturn(cardReset);
		CallbackError callbackError = new CallbackError();
		callbackError.setCode("500");
		callbackError.setMessage("Internal Server Error");
		BDDMockito.given(this.virtualCardsRepository.findByCardGuid(any())).willReturn(Optional.of(new VirtualCard()));
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("payment-id-123");
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(paymentDetails);
		paymentDetails.setId("payment-id-123");
		BDDMockito.given(this.paymentDetailsRepository.findById((String) any()))
			.willReturn(Optional.of(paymentDetails));
		BDDMockito.given(this.authExtractRepository.findByReceiptId(any())).willReturn(new AuthExtract());
		BDDMockito.given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(any()))
			.willReturn(Optional.of(new CardCreation()));
		BDDMockito.given(this.paymentRequestRepository.findByPaymentId(any())).willReturn(paymentRequest);
		this.webhookCallbackErrorHandler.process(callbackError, VIRTUAL_CARD_RESET);
	}

	@Test
	public void testHandleCallbackErrorResetNotFound() {
		BDDMockito.given(this.cardResetRepository.findByReceiptId(any())).willReturn(null);
		CallbackError callbackError = new CallbackError();
		callbackError.setCode("500");
		callbackError.setMessage("Internal Server Error");
		assertThatThrownBy(() -> this.webhookCallbackErrorHandler.process(callbackError, VIRTUAL_CARD_RESET))
			.isInstanceOf(IllegalArgumentException.class);
	}

	@Test
	public void testHandleCallbackErrorUnknownEventType() {
		CallbackError callbackError = new CallbackError();
		callbackError.setCode("500");
		callbackError.setMessage("Internal Server Error");
		callbackError.setClientRequestId(UUID.randomUUID());
		assertThatThrownBy(() -> this.webhookCallbackErrorHandler.process(callbackError, "UNKNOWN_EVENT_TYPE"))
			.isInstanceOf(IllegalArgumentException.class);
	}

	@Test
	public void testHandleCallbackErrorJsonProcessingException() throws Exception {
		CardCreation cardCreation = new CardCreation();
		cardCreation.setPaymentId("12345678");
		Mockito.lenient().when(this.virtualCardUtil.getCardCreationByRequestId(any())).thenReturn(cardCreation);
		CallbackError callbackError = new CallbackError();
		callbackError.setCode("500");
		callbackError.setMessage("Internal Server Error");
		callbackError.setClientRequestId(UUID.randomUUID());
		PaymentDetails paymentDetails = new PaymentDetails();
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setId(Long.valueOf("12345678"));
		paymentRequest.setPaymentDetails(paymentDetails);
		paymentDetails.setId("f1a8f5fa-87eb-4060-918b-9289a23d4e35");
		Mockito.lenient()
			.when(this.virtualCardUtil.updatePaymentRequestStatus(any(), any(), any()))
			.thenReturn(paymentRequest);

	}

}
