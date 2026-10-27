package com.uhg.optum.financials.payments.payer.job.task;

import lombok.extern.slf4j.Slf4j;
import org.springframework.batch.core.ExitStatus;
import org.springframework.batch.core.listener.StepExecutionListener;
import org.springframework.batch.core.step.StepExecution;

@Slf4j
public class TransactionCountListener implements StepExecutionListener {

	@Override
	public ExitStatus afterStep(StepExecution stepExecution) {
		// Each commit in a chunk is a transaction commit.
		log.debug("Total transactions (commits): {}", stepExecution.getCommitCount());
		return stepExecution.getExitStatus();
	}

}
