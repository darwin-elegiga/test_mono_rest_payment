package com.uhg.optum.financials.payments.payer.job;

import org.springframework.batch.core.repository.JobRepository;
import org.springframework.context.annotation.Configuration;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.batch.core.job.parameters.JobParameters;
import org.springframework.batch.core.job.parameters.JobParametersBuilder;
import org.springframework.batch.core.launch.JobOperator;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.ECC_KEY_ROTATION;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.JOB_ID;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.KEY_NAME;
import static com.uhg.optum.financials.payments.payer.constant.JobName.ECC_KEY_ROTATION_JOB;

@Configuration
public class ECCKeyRotationJob implements BaseJob {

	private final JobOperator jobOperator;

	private final JobRepository jobRepository;

	private final PlatformTransactionManager transactionManager;

	public ECCKeyRotationJob(JobOperator jobOperator, JobRepository jobRepository,
			PlatformTransactionManager transactionManager) {
		this.jobOperator = jobOperator;
		this.jobRepository = jobRepository;
		this.transactionManager = transactionManager;
	}

	@Override
	public String run(Object... args) {
		synchronized (this) {
			this.checkJobAlreadyRunningOrNot(this.jobRepository, ECC_KEY_ROTATION_JOB);
			String jobId = String.valueOf(System.currentTimeMillis());

			String keyName = ECC_KEY_ROTATION + System.currentTimeMillis();

			JobParameters params = new JobParametersBuilder().addString(JOB_ID, jobId)
				.addString(KEY_NAME, keyName)
				.toJobParameters();

			return jobId;
		}
	}

}
