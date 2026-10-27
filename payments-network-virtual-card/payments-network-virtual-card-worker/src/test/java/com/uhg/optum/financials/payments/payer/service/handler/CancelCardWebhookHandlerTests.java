package com.uhg.optum.financials.payments.payer.service.handler;

import com.uhg.optum.financials.payments.payer.dto.callback.CancelCardData;
import com.uhg.optum.financials.payments.payer.model.*;
import com.uhg.optum.financials.payments.payer.repository.CardCancellationRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Optional;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
public class CancelCardWebhookHandlerTests {

	@Mock
	CardCancellationRepository cardCancellationRepository;

	@Mock
	VirtualCardsRepository virtualCardsRepository;

	@Mock
	AuditLogger auditLogger;

	@Mock
	WebhookClient webhookClient;

	@Mock
	PaymentDetailsRepository paymentDetailsRepository;

	@Mock
	LogUtility logUtility;

	@Mock
	VirtualCardUtil virtualCardUtil;

	@Test
	public void testHandleCancel() {
		CancelCardWebhookHandler cancelCardWebhookHandler = new CancelCardWebhookHandler(
				this.cardCancellationRepository, this.logUtility, this.auditLogger, this.virtualCardsRepository,
				this.paymentDetailsRepository, this.virtualCardUtil, this.webhookClient);

		CardCancellation cardCancellation = new CardCancellation();
		cardCancellation.setCardGuid(UUID.randomUUID());
		cardCancellation.setPaymentId("12345678");
		BDDMockito.when(this.cardCancellationRepository.findById(any())).thenReturn(Optional.of(cardCancellation));

		CardCreation cardCreation = new CardCreation();
		cardCreation.setStatus("READY");
		BDDMockito.when(this.virtualCardUtil.getCardCreationByRequestId(any())).thenReturn(cardCreation);

		CancelCardData cancelCardData = new CancelCardData();
		cancelCardData.setCardGuid(cardCancellation.getCardGuid());

		VirtualCard virtualCard = new VirtualCard();
		virtualCard.setCardGuid(cardCancellation.getCardGuid());
		BDDMockito.when(this.virtualCardsRepository.findByPaymentId(any())).thenReturn(virtualCard);
		BDDMockito.when(this.virtualCardsRepository.findByPaymentId("12345678")).thenReturn(virtualCard);

		PaymentRequest paymentRequest = new PaymentRequest();
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("12345678");
		paymentRequest.setPaymentDetails(paymentDetails);
		BDDMockito.when(this.virtualCardUtil.updatePaymentRequestStatus(any(), any())).thenReturn(paymentRequest);
		BDDMockito.when(this.paymentDetailsRepository.findById("12345678")).thenReturn(Optional.of(paymentDetails));
		BDDMockito.when(this.virtualCardsRepository.findByCardGuid(any())).thenReturn(Optional.of(virtualCard));
		cancelCardWebhookHandler.process(cancelCardData);
		Mockito.verify(this.virtualCardsRepository, Mockito.times(1)).save(any());
	}

	@Test
	public void testHandleCancel_ExceptionLogging() {
		CancelCardWebhookHandler cancelCardWebhookHandler = new CancelCardWebhookHandler(
				this.cardCancellationRepository, this.logUtility, this.auditLogger, this.virtualCardsRepository,
				this.paymentDetailsRepository, this.virtualCardUtil, this.webhookClient);

		com.uhg.optum.financials.payments.payer.model.CardCreation cardCreation = new com.uhg.optum.financials.payments.payer.model.CardCreation();
		cardCreation.setStatus("READY");
		BDDMockito.when(this.virtualCardUtil.getCardCreationByRequestId(any())).thenReturn(cardCreation);

		CancelCardData cancelCardData = new CancelCardData();
		cancelCardData.setCardGuid(UUID.randomUUID());
		// Simulate repository throwing exception
		BDDMockito.when(this.cardCancellationRepository.findById(any())).thenThrow(new RuntimeException("DB error"));

		cancelCardWebhookHandler.process(cancelCardData);

		Mockito.verify(this.auditLogger)
			.logException(Mockito.any(), Mockito.any(), Mockito.eq("UNKNOWN"), Mockito.any(), Mockito.any(),
					Mockito.any());

		Mockito.verify(this.logUtility)
			.logMessage(
					Mockito
						.contains("Error while processing cancel card for cardGuid: " + cancelCardData.getCardGuid()),
					Mockito.eq(ERROR), Mockito.any());
		Mockito.verify(this.logUtility)
			.logMessageWithPriority(
					Mockito.contains("Virtual card void failed for cardGuid: " + cancelCardData.getCardGuid()),
					Mockito.eq(ERROR), Mockito.any(), Mockito.eq(P3));
	}

}
