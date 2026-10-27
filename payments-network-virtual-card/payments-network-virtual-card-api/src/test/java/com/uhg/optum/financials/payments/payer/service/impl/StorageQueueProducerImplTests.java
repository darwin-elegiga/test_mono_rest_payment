package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.exception.QueuePublishException;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.worker.service.IWorkerPublisher;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.BDDMockito.then;
import static org.mockito.BDDMockito.willThrow;

@ExtendWith(MockitoExtension.class)
public class StorageQueueProducerImplTests {

	@Mock
	private LogUtility logUtility;

	@Mock
	private IWorkerPublisher publisher;

	@InjectMocks
	private StorageQueueProducerImpl storageQueueProducerImpl;

	@Test
	void sendPaymentEventToQueue_success() {
		PaymentRequestDto dto = new PaymentRequestDto();
		PaymentDetailsDto details = new PaymentDetailsDto();
		details.setId("payId");
		dto.setPaymentDetails(details);
		this.storageQueueProducerImpl.sendPaymentEventToQueue(dto);
		then(this.publisher).should().publishEvent(dto);
		then(this.publisher).shouldHaveNoMoreInteractions();
	}

	@Test
	void sendPaymentEventToQueue_exception_throwsQueuePublishException() {
		PaymentRequestDto dto = new PaymentRequestDto();
		PaymentDetailsDto details = new PaymentDetailsDto();
		details.setId("payId");
		dto.setPaymentDetails(details);
		willThrow(new RuntimeException("fail!")).given(this.publisher).publishEvent(dto);

		assertThatThrownBy(() -> this.storageQueueProducerImpl.sendPaymentEventToQueue(dto))
			.isInstanceOf(QueuePublishException.class);

		then(this.logUtility).should()
			.logMessageWithPriority(
					org.mockito.ArgumentMatchers.startsWith("Exception while sending the payment message to the queue"),
					eq("error"), any(), eq("P3"));
	}

	@Test
	void sendVirtualCardEventToQueue_success() {
		ZetaWebhookDto<Object> dto = new ZetaWebhookDto<>("id123", "1.0",
				java.time.Instant.parse("2025-11-19T12:27:05.157744Z"), "status", "type", new Object(), null);
		this.storageQueueProducerImpl.sendVirtualCardEventToQueue(dto);
		then(this.publisher).should().publishEvent(dto);
		then(this.publisher).shouldHaveNoMoreInteractions();
	}

	@Test
	void sendVirtualCardEventToQueue_exception_throwsQueuePublishException() {
		ZetaWebhookDto<Object> dto = new ZetaWebhookDto<>("id123", "1.0",
				java.time.Instant.parse("2025-11-19T12:27:05.157744Z"), "status", "type", new Object(), null);
		willThrow(new RuntimeException("fail!")).given(this.publisher).publishEvent(dto);

		assertThatThrownBy(() -> this.storageQueueProducerImpl.sendVirtualCardEventToQueue(dto))
			.isInstanceOf(QueuePublishException.class);

		then(this.logUtility).should()
			.logMessageWithPriority(
					org.mockito.ArgumentMatchers
						.startsWith("Exception while sending the create virtual card message to the queue"),
					eq("error"), any(), eq("P3"));
	}

	@Test
	void sendPaymentEventToQueue_doesNotCheckQueueHealthBeforePublishing() {
		PaymentRequestDto dto = this.createPaymentRequest();

		this.storageQueueProducerImpl.sendPaymentEventToQueue(dto);

		then(this.publisher).should().publishEvent(dto);
		then(this.publisher).shouldHaveNoMoreInteractions();
	}

	@Test
	void sendVirtualCardEventToQueue_doesNotCheckQueueHealthBeforePublishing() {
		ZetaWebhookDto<Object> dto = new ZetaWebhookDto<>("id123", "1.0",
				java.time.Instant.parse("2025-11-19T12:27:05.157744Z"), "status", "type", new Object(), null);

		this.storageQueueProducerImpl.sendVirtualCardEventToQueue(dto);

		then(this.publisher).should().publishEvent(dto);
		then(this.publisher).shouldHaveNoMoreInteractions();
	}

	private PaymentRequestDto createPaymentRequest() {
		PaymentRequestDto dto = new PaymentRequestDto();
		PaymentDetailsDto details = new PaymentDetailsDto();
		details.setId("payId");
		dto.setPaymentDetails(details);
		return dto;
	}

}
