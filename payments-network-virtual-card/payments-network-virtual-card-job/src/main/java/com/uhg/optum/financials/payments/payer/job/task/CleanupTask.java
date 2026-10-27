package com.uhg.optum.financials.payments.payer.job.task;

import com.uhg.optum.financials.payments.payer.enums.Status;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import lombok.extern.slf4j.Slf4j;

import org.springframework.batch.core.scope.context.ChunkContext;
import org.springframework.batch.core.step.StepContribution;
import org.springframework.batch.core.step.tasklet.Tasklet;
import org.springframework.batch.infrastructure.repeat.RepeatStatus;

/***
 * Description: This class handles cleaning data which is not required after payment
 * lifecycle completion.
 *
 * @author adhiman5, created on 18-July-2025
 *
 */

@Slf4j
public class CleanupTask implements Tasklet {

	private final PaymentRequestRepository paymentRequestRepository;

	private final String correlationId;

	public CleanupTask(PaymentRequestRepository paymentRequestRepository, String correlationId) {
		this.paymentRequestRepository = paymentRequestRepository;
		this.correlationId = correlationId;
	}

	@Override
	public RepeatStatus execute(StepContribution contribution, ChunkContext chunkContext) throws Exception {

		int stSeCleanedUp = this.paymentRequestRepository.nullifyStSeSegmentForCorrelationId(this.correlationId,
				Status.PS.name());

		log.info("{} st_se segment cleanup completed for correlationId: {}", stSeCleanedUp, this.correlationId);

		return RepeatStatus.FINISHED;
	}

}
