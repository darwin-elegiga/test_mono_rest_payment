package com.uhg.optum.financials.payments.payer.job.task;

import com.uhg.optum.financials.payments.payer.enums.Status;
import com.uhg.optum.financials.payments.payer.enums.VirtualCardState;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import lombok.extern.slf4j.Slf4j;
import org.springframework.batch.core.configuration.annotation.JobScope;
import org.springframework.batch.core.scope.context.ChunkContext;
import org.springframework.batch.core.step.StepContribution;
import org.springframework.batch.core.step.tasklet.Tasklet;
import org.springframework.batch.infrastructure.repeat.RepeatStatus;

import java.util.List;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BATCH_DETAIL_STATUS_835_CREATED;

@Slf4j
@JobScope
public class CompanionFileTask implements Tasklet {

	private final List<String> eligibleCorrelationIds;

	private final PaymentRequestRepository paymentRequestRepository;

	public CompanionFileTask(PaymentRequestRepository paymentRequestRepository, List<String> eligibleCorrelationIds) {
		this.paymentRequestRepository = paymentRequestRepository;
		this.eligibleCorrelationIds = eligibleCorrelationIds;
	}

	@Override
	public RepeatStatus execute(StepContribution contribution, ChunkContext chunkContext) {
		log.info("Finding eligible correlation IDs...");
		// Reset carry-over state from previous runs before adding the latest eligible
		// IDs.
		this.eligibleCorrelationIds.clear();
		List<String> eligibleCorrelationIdsFromDb = this.paymentRequestRepository.findEligibleCompanionCorrelationIds(
				BATCH_DETAIL_STATUS_835_CREATED, Status.PC.name(), VirtualCardState.CREATED.name());
		this.eligibleCorrelationIds.addAll(eligibleCorrelationIdsFromDb);
		log.info("Eligible correlation Id list size is: {}", this.eligibleCorrelationIds.size());

		return RepeatStatus.FINISHED;
	}

}
