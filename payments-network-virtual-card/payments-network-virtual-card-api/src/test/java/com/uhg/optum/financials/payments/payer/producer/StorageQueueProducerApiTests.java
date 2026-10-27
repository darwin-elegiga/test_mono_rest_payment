package com.uhg.optum.financials.payments.payer.producer;

import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.core.beans.HealthDetail;
import com.uhg.optum.financials.shared.worker.service.IWorkerPublisher;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.LocalDateTime;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.FAILED;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.SUCCESS;
import static org.mockito.Mockito.doNothing;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;

@ExtendWith(MockitoExtension.class)
public class StorageQueueProducerApiTests {

	@Mock
	private IWorkerPublisher publisher;

	@InjectMocks
	private StorageQueueProducerApi storageQueueProducer;

	@Test
	public void testSendMessageToQueueSuccess() {
		PaymentRequestDto loadPaymentRequest = new PaymentRequestDto();
		doNothing().when(this.publisher).publishEvent(Mockito.any(PaymentRequestDto.class));
		String result = this.storageQueueProducer.sendMessageToQueue(loadPaymentRequest);
		Assertions.assertThat(result).isEqualTo(SUCCESS);
		verify(this.publisher, times(1)).publishEvent(loadPaymentRequest);
	}

	@Test
	public void testSendMessageToQueueFailure() {
		PaymentRequestDto loadPaymentRequest = new PaymentRequestDto();
		BDDMockito.doThrow(new RuntimeException("Error while sending message to queue"))
			.when(this.publisher)
			.publishEvent(loadPaymentRequest);
		String result = this.storageQueueProducer.sendMessageToQueue(loadPaymentRequest);
		Assertions.assertThat(result).isEqualTo(FAILED);
		verify(this.publisher, times(1)).publishEvent(loadPaymentRequest);
	}

	@Test
	public void testCheckHealthPublisherHealthy() {
		HealthDetail expectedHealth = new HealthDetail(HealthDetail.HealthStatus.UP, "producer is up.",
				LocalDateTime.now());
		Mockito.when(this.publisher.getHealth()).thenReturn(expectedHealth);

		HealthDetail actualHealth = this.storageQueueProducer.checkHealthPublisher();

		Assertions.assertThat(actualHealth.getStatus()).isEqualTo(HealthDetail.HealthStatus.UP);
	}

	@Test
	public void testCheckHealthPublisherUnhealthy() {
		HealthDetail expectedHealth = new HealthDetail(HealthDetail.HealthStatus.DOWN,
				"Could not connect to storage queue.", LocalDateTime.now());
		Mockito.when(this.publisher.getHealth()).thenReturn(expectedHealth);

		HealthDetail actualHealth = this.storageQueueProducer.checkHealthPublisher();

		Assertions.assertThat(actualHealth.getStatus()).isEqualTo(HealthDetail.HealthStatus.DOWN);
	}

}
