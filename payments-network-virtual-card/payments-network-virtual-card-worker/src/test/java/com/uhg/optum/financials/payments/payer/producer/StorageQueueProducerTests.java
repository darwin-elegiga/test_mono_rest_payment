package com.uhg.optum.financials.payments.payer.producer;

import com.uhg.optum.financials.shared.core.beans.HealthDetail;
import com.uhg.optum.financials.shared.worker.service.IWorkerPublisher;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
public class StorageQueueProducerTests {

	@InjectMocks
	private StorageQueueProducer storageQueueProducer;

	@Mock
	private IWorkerPublisher publisher;

	@Test
	void testSendMessageToQueue() {
		Assertions.assertThat(this.storageQueueProducer.sendMessageToQueue("test")).isEqualTo("SUCCESS");
	}

	@Test
	void testSendMessageToQueueException() {
		Mockito.doThrow(new RuntimeException()).when(this.publisher).publishEvent(Mockito.any());
		Assertions.assertThat(this.storageQueueProducer.sendMessageToQueue("test")).isEqualTo("FAILED");
	}

	@Test
	void testCheckHealthPublisher() {
		HealthDetail healthDetail = Mockito.mock(HealthDetail.class);
		Mockito.when(this.publisher.getHealth()).thenReturn(healthDetail);
		Assertions.assertThat(this.storageQueueProducer.checkHealthPublisher()).isNotNull();
	}

}
