package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.ApiConstants;
import com.uhg.optum.financials.payments.payer.dto.callback.CallbackError;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.CancelCardService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.ProcessFailedEventUtil;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.Instant;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;

@ExtendWith(MockitoExtension.class)
class ProcessFailedEventServiceImplTests {

	@Mock
	private ProcessFailedEventUtil processFailedEventUtil;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@Mock
	private CardCreationRepository cardCreationRepository;

	@Mock
	private LogUtility logUtility;

	@Mock
	private com.uhg.optum.financials.payments.payer.service.ZetaWebhookService zetaWebhookService;

	@Mock
	private CancelCardService cancelCardService;

	private ProcessFailedEventServiceImpl service;

	@BeforeEach
	void setUp() {
		this.service = new ProcessFailedEventServiceImpl(this.processFailedEventUtil, this.paymentRequestRepository,
				this.cardCreationRepository, this.logUtility, this.zetaWebhookService, 5, 15, 100, 60L, 1000L,
				this.cancelCardService);
	}

	@AfterEach
	void tearDown() throws Exception {
		this.service.destroy();
	}

	@Test
	void getClientRequestID_returnsClientRequestId() {
		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(UUID.fromString("11111111-1111-1111-1111-111111111111"));
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc("payment-1"))
			.willReturn(Optional.of(cardCreation));

		String result = this.service.getClientRequestID("payment-1");

		assertThat(result).isEqualTo("11111111-1111-1111-1111-111111111111");
		verify(this.cardCreationRepository).findTopByPaymentIdOrderByCreatedAtDesc("payment-1");
	}

	@Test
	void callCardPooling_returnsNullWhenRestCallFails() {
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc("payment-1"))
			.willReturn(Optional.empty());

		String result = this.service.getClientRequestID("payment-1");

		assertThat(result).isNull();
		verify(this.cardCreationRepository).findTopByPaymentIdOrderByCreatedAtDesc("payment-1");
	}

	@Test
	void processFailedEvent_withPaymentId_postsWebhookWithCardPoolingResult() {
		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(UUID.fromString("22222222-2222-2222-2222-222222222222"));
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc("payment-1"))
			.willReturn(Optional.of(cardCreation));
		given(this.zetaWebhookService.processZetaWebhook(any())).willReturn(1);

		this.service.processFailedEvent("payment-1", null);

		verify(this.processFailedEventUtil).checkIfPaymentIdNotAvailable("payment-1");
		verify(this.cardCreationRepository).findTopByPaymentIdOrderByCreatedAtDesc("payment-1");

		@SuppressWarnings("unchecked")
		ArgumentCaptor<ZetaWebhookDto<?>> payloadCaptor = ArgumentCaptor
			.forClass((Class<ZetaWebhookDto<?>>) (Class<?>) ZetaWebhookDto.class);
		verify(this.zetaWebhookService, times(1)).processZetaWebhook(payloadCaptor.capture());

		List<ZetaWebhookDto<?>> payloads = payloadCaptor.getAllValues();
		assertThat(payloads).hasSize(1);
		assertThat(payloads).allSatisfy((payload) -> {
			assertThat(payload.specversion()).isEqualTo(ApiConstants.SPEC_VERSION);
			assertThat(payload.status()).isEqualTo(ApiConstants.EVENT_STATUS);
			assertThat(payload.time()).isInstanceOf(Instant.class);
		});
		assertThat(payloads).extracting(ZetaWebhookDto::type).containsExactly(ApiConstants.EVENT_TYPE);

		assertThat(payloads)
			.extracting((ZetaWebhookDto<?> payload) -> ((CallbackError) payload.error()).getClientRequestId())
			.containsExactly(UUID.fromString("22222222-2222-2222-2222-222222222222"));
		assertThat(payloads).extracting((ZetaWebhookDto<?> payload) -> (CallbackError) payload.error())
			.allSatisfy((CallbackError error) -> {
				assertThat(error.getCode()).isEqualTo(ApiConstants.ERRORCODE);
				assertThat(error.getMessage()).isEqualTo(ApiConstants.MESSAGE);
				assertThat(error.getReceiptId()).isEqualTo("");
			});
	}

	@Test
	void processFailedEvent_withCorrelationIdProcessesAllPaymentIdsAndPublishesEachPayload() {
		given(this.paymentRequestRepository.findByCorrelationId("correlation-1"))
			.willReturn(List.of(paymentRequest("payment-1"), paymentRequest("payment-2")));
		CardCreation cardCreation1 = new CardCreation();
		cardCreation1.setId(UUID.fromString("33333333-3333-3333-3333-333333333333"));
		CardCreation cardCreation2 = new CardCreation();
		cardCreation2.setId(UUID.fromString("44444444-4444-4444-4444-444444444444"));
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc("payment-1"))
			.willReturn(Optional.of(cardCreation1));
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc("payment-2"))
			.willReturn(Optional.of(cardCreation2));
		given(this.zetaWebhookService.processZetaWebhook(any())).willReturn(1);

		this.service.processFailedEvent(null, "correlation-1");

		verify(this.processFailedEventUtil, never()).checkIfPaymentIdNotAvailable("payment-1");
		verify(this.processFailedEventUtil, never()).checkIfPaymentIdNotAvailable("payment-2");
		verify(this.paymentRequestRepository).findByCorrelationId("correlation-1");
		verify(this.cardCreationRepository).findTopByPaymentIdOrderByCreatedAtDesc("payment-1");
		verify(this.cardCreationRepository).findTopByPaymentIdOrderByCreatedAtDesc("payment-2");
		verify(this.zetaWebhookService, times(2)).processZetaWebhook(any());

		@SuppressWarnings("unchecked")
		ArgumentCaptor<ZetaWebhookDto<?>> payloadCaptor = ArgumentCaptor
			.forClass((Class<ZetaWebhookDto<?>>) (Class<?>) ZetaWebhookDto.class);
		verify(this.zetaWebhookService, times(2)).processZetaWebhook(payloadCaptor.capture());

		List<ZetaWebhookDto<?>> payloads = payloadCaptor.getAllValues();
		assertThat(payloads).hasSize(2);
		assertThat(payloads)
			.extracting((ZetaWebhookDto<?> payload) -> ((CallbackError) payload.error()).getClientRequestId())
			.containsExactlyInAnyOrder(UUID.fromString("33333333-3333-3333-3333-333333333333"),
					UUID.fromString("44444444-4444-4444-4444-444444444444"));
	}

	@Test
	void processFailedEvent_withCorrelationIdAndNoMatchingPayments_rejectsUnknownCorrelationId() {
		given(this.paymentRequestRepository.findByCorrelationId("correlation-empty")).willReturn(List.of());

		assertThatThrownBy(() -> this.service.processFailedEvent(null, "correlation-empty"))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessage("No payments found for correlationId: correlation-empty");

		verify(this.paymentRequestRepository).findByCorrelationId("correlation-empty");
		verify(this.cardCreationRepository, never()).findTopByPaymentIdOrderByCreatedAtDesc("payment-1");
		verify(this.zetaWebhookService, never()).processZetaWebhook(any());
	}

	private static PaymentRequest paymentRequest(String paymentId) {
		PaymentRequest paymentRequest = new PaymentRequest();
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId(paymentId);
		paymentRequest.setPaymentDetails(paymentDetails);
		return paymentRequest;
	}

}
