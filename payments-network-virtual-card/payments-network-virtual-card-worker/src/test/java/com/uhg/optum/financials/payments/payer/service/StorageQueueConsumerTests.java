package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.handler.WorkerHandler;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.worker.event.WorkerMessage;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
class StorageQueueConsumerTests {

	@InjectMocks
	StorageQueueConsumer storageQueueConsumer;

	@Mock
	WorkerHandler storageQueueHandlers;

	@Mock
	private LogUtility logUtility;

	@Test
	void testGetConsumer() {

		WorkerMessage workerMessage = Mockito.mock(WorkerMessage.class);
		this.storageQueueConsumer.getConsumer().accept(workerMessage);
		Mockito.verify(this.storageQueueHandlers).handle(workerMessage);
	}

	@Test
	void testGetConsumerException() {
		// Arrange
		WorkerMessage<?> workerMessage = Mockito.mock(WorkerMessage.class);
		RuntimeException expectedException = new RuntimeException("Error occurred while processing message");
		Mockito.doThrow(expectedException).when(this.storageQueueHandlers).handle(any());

		// Act and Assert
		Assertions.assertThatThrownBy(() -> this.storageQueueConsumer.getConsumer().accept(workerMessage))
			.isInstanceOf(RuntimeException.class)
			.hasMessage(expectedException.getMessage());
	}

	@Test
	void testErrorConsumer() {
		Throwable throwable = Mockito.mock(Throwable.class);
		this.storageQueueConsumer.getErrorConsumer().accept(throwable);
		Assertions.assertThat(throwable).isNotNull();
	}

}
