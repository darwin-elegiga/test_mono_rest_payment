package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.enums.Status;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequestHistory;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.model.VirtualCardHistory;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestHistoryRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.ProcessedWebhookRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardHistoryRepository;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.math.BigDecimal;
import java.util.List;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;

@ExtendWith(MockitoExtension.class)
class VirtualCardUtilTests {

	@Mock
	private LogUtility logUtility;

	@Mock
	private CardCreationRepository cardCreationRepository;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@Mock
	private VirtualCardHistoryRepository virtualCardHistoryRepository;

	@InjectMocks
	private VirtualCardUtil virtualCardUtil;

	@Mock
	ProcessedWebhookRepository processedWebhookRepository;

	@Mock
	private PaymentRequestHistoryRepository paymentRequestHistoryRepository;

	@Test
	void getCardCreationByRequestId_success() {
		UUID requestId = UUID.randomUUID();
		CardCreation cardCreation = BDDMockito.mock(CardCreation.class);
		BDDMockito.given(this.cardCreationRepository.findById(requestId)).willReturn(Optional.of(cardCreation));
		CardCreation result = this.virtualCardUtil.getCardCreationByRequestId(requestId.toString());
		Assertions.assertThat(result).isEqualTo(cardCreation);
	}

	@Test
	void getCardCreationByRequestId_notFound() {
		UUID requestId = UUID.randomUUID();
		BDDMockito.given(this.cardCreationRepository.findById(requestId)).willReturn(Optional.empty());
		Assertions.assertThatThrownBy(() -> this.virtualCardUtil.getCardCreationByRequestId(requestId.toString()))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("CardCreation not found for requestId");
	}

	@Test
	void updatePaymentRequestStatus_success() {
		String paymentId = "pid";
		PaymentRequest paymentRequest = new PaymentRequest();
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentRequest.setPaymentDetails(paymentDetails);
		BDDMockito.given(this.paymentRequestRepository.findByPaymentId(paymentId)).willReturn(paymentRequest);
		BDDMockito.given(this.paymentRequestRepository.save(paymentRequest)).willReturn(paymentRequest);
		PaymentRequest result = this.virtualCardUtil.updatePaymentRequestStatus(paymentId, Status.PR);
		Assertions.assertThat(result.getStatus()).isEqualTo(Status.PR.name());
		Assertions.assertThat(result.getUpdatedById()).isEqualTo("VIRTUAL_CARD");
	}

	@Test
	void updatePaymentRequestStatus_notFound() {
		String paymentId = "pid";
		BDDMockito.given(this.paymentRequestRepository.findByPaymentId(paymentId)).willReturn(null);
		Assertions.assertThatThrownBy(() -> this.virtualCardUtil.updatePaymentRequestStatus(paymentId, Status.PR))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("PaymentRequest not found for requestId");
	}

	@Test
	void saveVirtualCardHistory_success() {
		VirtualCard virtualCard = new VirtualCard();
		virtualCard.setCardGuid(UUID.randomUUID());
		virtualCard.setStatus("ACTIVE");
		virtualCard.setPaymentId("pid");
		virtualCard.setInitialBalance(BigDecimal.valueOf(100.0));
		virtualCard.setAvailableBalance(BigDecimal.valueOf(80.0));
		virtualCard.setCurrentBalance(BigDecimal.valueOf(80.0));
		virtualCard.setCurrency("USD");
		String reasonCode = "REASON";
		BDDMockito.given(this.virtualCardHistoryRepository.save(BDDMockito.any(VirtualCardHistory.class)))
			.willReturn(new VirtualCardHistory());
		this.virtualCardUtil.saveVirtualCardHistory(virtualCard, reasonCode);
		BDDMockito.verify(this.virtualCardHistoryRepository).save(BDDMockito.any(VirtualCardHistory.class));
	}

	@Test
	void tryMarkWebhookProcessed_returnsTrue_whenClaimIsInserted() {
		String id = "webhook-id";
		String eventType = "EVENT_TYPE";
		BDDMockito.given(this.processedWebhookRepository.tryInsertProcessed(id, eventType, "PROCESSED")).willReturn(1);

		boolean result = this.virtualCardUtil.tryMarkWebhookProcessed(id, eventType);

		Assertions.assertThat(result).isTrue();
		BDDMockito.then(this.processedWebhookRepository).should().tryInsertProcessed(id, eventType, "PROCESSED");
	}

	@Test
	void tryMarkWebhookProcessed_returnsFalse_whenClaimAlreadyExists() {
		String id = "webhook-id";
		String eventType = "EVENT_TYPE";
		BDDMockito.given(this.processedWebhookRepository.tryInsertProcessed(id, eventType, "PROCESSED")).willReturn(0);

		boolean result = this.virtualCardUtil.tryMarkWebhookProcessed(id, eventType);

		Assertions.assertThat(result).isFalse();
		BDDMockito.then(this.processedWebhookRepository).should().tryInsertProcessed(id, eventType, "PROCESSED");
	}

	@Test
	void tryMarkWebhookProcessed_throwsException_whenInsertResultIsUnexpected() {
		String id = "webhook-id";
		String eventType = "EVENT_TYPE";
		BDDMockito.given(this.processedWebhookRepository.tryInsertProcessed(id, eventType, "PROCESSED")).willReturn(2);

		Assertions.assertThatThrownBy(() -> this.virtualCardUtil.tryMarkWebhookProcessed(id, eventType))
			.isInstanceOf(IllegalStateException.class)
			.hasMessageContaining("Unexpected insert result [2]");
	}

	@Test
	void updatePaymentRequestStatus_propagatesRePreferenceFlagToHistory() {
		String paymentId = "pid";
		PaymentRequest paymentRequest = new PaymentRequest();
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentRequest.setPaymentDetails(paymentDetails);
		paymentRequest.setRePreferenceFlag(true);
		BDDMockito.given(this.paymentRequestRepository.findByPaymentId(paymentId)).willReturn(paymentRequest);
		BDDMockito.given(this.paymentRequestRepository.save(paymentRequest)).willReturn(paymentRequest);
		ArgumentCaptor<PaymentRequestHistory> captor = ArgumentCaptor.forClass(PaymentRequestHistory.class);

		this.virtualCardUtil.updatePaymentRequestStatus(paymentId, Status.PR);

		BDDMockito.verify(this.paymentRequestHistoryRepository).save(captor.capture());
		Assertions.assertThat(captor.getValue().isRePreferenceFlag()).isTrue();
	}

	@Test
	void savePaymentRequestHistoryForIds_propagatesRePreferenceFlagToHistory() {
		PaymentRequest paymentRequest = new PaymentRequest();
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentRequest.setPaymentDetails(paymentDetails);
		paymentRequest.setRePreferenceFlag(true);
		BDDMockito.given(this.paymentRequestRepository.findByIdIn(BDDMockito.anySet()))
			.willReturn(List.of(paymentRequest));
		@SuppressWarnings("unchecked")
		ArgumentCaptor<List<PaymentRequestHistory>> captor = ArgumentCaptor.forClass(List.class);

		this.virtualCardUtil.savePaymentRequestHistoryForIds(Set.of("id-1"));

		BDDMockito.verify(this.paymentRequestHistoryRepository).saveAll(captor.capture());
		Assertions.assertThat(captor.getValue()).hasSize(1);
		Assertions.assertThat(captor.getValue().get(0).isRePreferenceFlag()).isTrue();
	}

}
