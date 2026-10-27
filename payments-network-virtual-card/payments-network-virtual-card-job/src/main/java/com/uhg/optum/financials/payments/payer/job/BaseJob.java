package com.uhg.optum.financials.payments.payer.job;

import com.uhg.optum.financials.payments.payer.constant.JobName;
import com.uhg.optum.financials.payments.payer.exception.JobAlreadyRunningException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.batch.core.job.Job;
import org.springframework.batch.core.job.JobExecution;
import org.springframework.batch.core.job.JobInstance;
import org.springframework.batch.core.job.parameters.JobParameters;
import org.springframework.batch.core.launch.JobOperator;
import org.springframework.batch.core.launch.support.TaskExecutorJobOperator;
import org.springframework.batch.core.repository.JobRepository;
import org.springframework.dao.DataAccessResourceFailureException;

import static com.uhg.optum.financials.payments.payer.constant.DocumentConstants.JOB_RUNNING_MSG;
import static com.uhg.optum.financials.payments.payer.constant.JobStatus.FAILED;

public interface BaseJob {

	Logger log = LoggerFactory.getLogger(BaseJob.class);

	String run(Object... args);

	default void checkJobAlreadyRunningOrNot(JobRepository jobRepository, JobName jobName) {
		this.checkJobAlreadyRunningOrNot(jobRepository, jobName.name());
	}

	default void checkJobAlreadyRunningOrNot(JobRepository jobRepository, String jobName) {
		JobInstance jobInstance = jobRepository.getLastJobInstance(jobName);
		if (jobInstance != null
				&& jobRepository.getJobExecutions(jobInstance).stream().anyMatch(JobExecution::isRunning)) {
			throw new JobAlreadyRunningException(JOB_RUNNING_MSG);
		}
	}

	default void launchJob(JobOperator jobOperator, String jobId, JobParameters params, Job job) {
		try {
			log.info("Job ID : {}, Ready to Launch.", jobId);
			JobExecution jobExecution = jobOperator.start(job, params);
			log.info("Job ID : {}, Completed.", jobId);
			if (jobExecution.getExitStatus().getExitCode().equalsIgnoreCase(FAILED)) {
				if (!jobExecution.getAllFailureExceptions().isEmpty()) {
					jobExecution.getAllFailureExceptions().forEach((t) -> log.error(t.getMessage()));
				}
				else {
					log.error("Job failed with exit code: {}", jobExecution.getExitStatus().getExitCode());
				}
			}
		}
		catch (DataAccessResourceFailureException dataAccessResourceFailureException) {
			log.error("Data access resource failure exception occurred: {}",
					dataAccessResourceFailureException.getMessage());
		}
		catch (Exception ex) {
			log.error("Exception occurred: {}", ex.getMessage());
		}
	}

	default void launchJobAsync(TaskExecutorJobOperator taskExecutorJobOperator, String jobId, JobParameters params,
			Job job) {
		try {
			log.info("Job ID : {}, Ready to Launch(Async).", jobId);
			taskExecutorJobOperator.start(job, params);
			log.info("Job ID : {}, Launched.", jobId);
		}
		catch (DataAccessResourceFailureException dataAccessResourceFailureException) {
			log.error("Async Data access resource failure exception occurred(Async): {}",
					dataAccessResourceFailureException.getMessage());
		}
		catch (Exception ex) {
			log.error("Exception occurred(Async): {}", ex.getMessage());
		}
	}

}
