package com.uhg.optum.financials.payments.payer.job;

import com.uhg.optum.financials.payments.payer.dto.CorrelatedPaymentDetail;
import com.uhg.optum.financials.payments.payer.job.param.Payment835JobParam;
import com.uhg.optum.financials.payments.payer.job.processor.SegmentProcessor;
import com.uhg.optum.financials.payments.payer.job.reader.SegmentReader;
import com.uhg.optum.financials.payments.payer.job.writer.PaymentFileWriter;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.projection.PaymentSegment;
import lombok.Getter;
import lombok.extern.slf4j.Slf4j;
import org.springframework.batch.core.job.builder.JobBuilder;
import org.springframework.batch.core.job.builder.SimpleJobBuilder;
import org.springframework.batch.core.repository.JobRepository;
import org.springframework.batch.core.step.builder.StepBuilder;
import org.springframework.batch.core.step.tasklet.TaskletStep;
import org.springframework.beans.factory.ObjectProvider;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.batch.core.job.parameters.JobParameters;
import org.springframework.batch.core.job.parameters.JobParametersBuilder;
import org.springframework.batch.core.step.Step;
import org.springframework.batch.infrastructure.repeat.RepeatStatus;
import org.springframework.batch.core.job.Job;
import java.util.Objects;

import static com.uhg.optum.financials.payments.payer.constant.JobConstant.CLEANUP_DATA;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.CONSOLIDATED_FILE_CREATION;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.PARAM;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.SENT;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;
import static com.uhg.optum.financials.payments.payer.util.ParameterProvider.toJsonString;

@Slf4j
public abstract class File835Job {

	@Getter
	private final JobRepository jobRepository;

	private final PlatformTransactionManager transactionManager;

	private final ObjectProvider<SegmentReader> segmentReaderProvider;

	private final ObjectProvider<SegmentProcessor> segmentProcessorProvider;

	private final ObjectProvider<PaymentFileWriter> paymentFileWriterProvider;

	private final LogUtility logUtility;

	protected File835Job(JobRepository jobRepository, PlatformTransactionManager transactionManager,
			ObjectProvider<SegmentReader> segmentReaderProvider,
			ObjectProvider<SegmentProcessor> segmentProcessorProvider,
			ObjectProvider<PaymentFileWriter> paymentFileWriterProvider, LogUtility logUtility) {
		this.jobRepository = jobRepository;
		this.transactionManager = transactionManager;
		this.segmentReaderProvider = segmentReaderProvider;
		this.segmentProcessorProvider = segmentProcessorProvider;
		this.paymentFileWriterProvider = paymentFileWriterProvider;
		this.logUtility = logUtility;
	}

	public JobParameters buildJobParam(String jobId, CorrelatedPaymentDetail cpr) {
		return buildJobParam(jobId, cpr, cpr.getCorrelationId());
	}

	public JobParameters buildJobParam(String jobId, CorrelatedPaymentDetail cpr, String outboundCorrelationId) {
		Payment835JobParam param = new Payment835JobParam();
		param.setJobId(jobId);
		param.setEventType(SENT);
		param.setCorrelationId(outboundCorrelationId);
		return new JobParametersBuilder()
			.addString(PARAM, Objects.requireNonNull(toJsonString(param), "Failed to convert job parameters to JSON"))
			.addLong("run.id", System.currentTimeMillis())
			.toJobParameters();
	}

	public Job consolidatedFileJob(String jobName, int chunkSize) {
		SimpleJobBuilder jobBuilder = new JobBuilder(jobName, this.jobRepository)
			// .incrementer(new RunIdIncrementer())
			.start(this.consolidatedFileStep(chunkSize));
		jobBuilder.next(this.cleanupTask());
		return jobBuilder.build();
	}

	private Step consolidatedFileStep(int chunkSize) {
		return new StepBuilder(CONSOLIDATED_FILE_CREATION, this.jobRepository)
			.<PaymentSegment, PaymentSegment>chunk(chunkSize)
			.reader(this.segmentReaderProvider.getObject())
			.processor(this.segmentProcessorProvider.getObject())
			.writer(this.paymentFileWriterProvider.getObject())
			.transactionManager(this.transactionManager)
			.build();
	}

	private TaskletStep cleanupTask() {
		return new StepBuilder(CLEANUP_DATA, this.jobRepository).tasklet((contribution, chunkContext) -> {
			this.logUtility.logMessage("Clean up data.", INFO, log);
			return RepeatStatus.FINISHED;
		}, this.transactionManager).build();
	}

}
