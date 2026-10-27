package com.uhg.optum.financials.payments.payer.job.task;

import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
public class CleanupTaskTests {

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@InjectMocks
	private CleanupTask cleanupTask;

	@Test
	public void testExecute() throws Exception {
		String correlationId = "test-correlation-id";
		this.cleanupTask = new CleanupTask(this.paymentRequestRepository, correlationId);

		// Execute the task
		this.cleanupTask.execute(null, null);

		// Verify that the repository method was called with the correct parameters
		Mockito.verify(this.paymentRequestRepository).nullifyStSeSegmentForCorrelationId(correlationId, "PS");
	}

}
