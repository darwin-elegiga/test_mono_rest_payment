package com.uhg.optum.financials.payments.payer.job;

import com.uhg.optum.financials.payments.payer.constant.JobName;
import com.uhg.optum.financials.payments.payer.dto.CorrelatedPaymentDetail;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelatedBatchDetail;
import com.uhg.optum.financials.payments.payer.enums.VirtualCardState;
import com.uhg.optum.financials.payments.payer.job.processor.SegmentProcessor;
import com.uhg.optum.financials.payments.payer.job.reader.SegmentReader;
import com.uhg.optum.financials.payments.payer.job.writer.PaymentFileWriter;
import com.uhg.optum.financials.payments.payer.enums.Status;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.springframework.batch.core.configuration.JobRegistry;
import org.springframework.batch.core.launch.support.TaskExecutorJobOperator;
import org.springframework.batch.core.repository.JobRepository;
import org.springframework.beans.factory.ObjectProvider;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Configuration;
import org.springframework.scheduling.concurrent.ThreadPoolTaskExecutor;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.util.ObjectUtils;

import java.util.Objects;
import java.util.stream.Collectors;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BATCH_DETAIL_STATUS_FAILED;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BATCH_DETAIL_STATUS_INITIALIZE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BATCH_DETAIL_STATUS_PROGRESS;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.AMOUNT_NOT_MATCH;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.PAYMENT_COUNT_NOT_MATCH;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;

@Slf4j
@Configuration
public class Payment835Job extends File835Job implements BaseJob {

	@Value("${alert.time.835}")
	private long alertTimeFor835File;

	@Value("${com.optum.job.chunkSize}")
	private int chunkSize;

	private final JobRegistry jobRegistry;

	private final JobRepository jobRepository;

	private final BatchDetailService batchDtlSvc;

	private final PaymentRequestRepository paymentReqRepo;

	private final ThreadPoolTaskExecutor taskExecutor;

	private final LogUtility logUtility;

	public static final String ALERT_MESSAGE = "Raising P3 alert: ";

	public Payment835Job(JobRepository jobRepository, PlatformTransactionManager transactionManager,
			ObjectProvider<SegmentReader> segmentReaderProvider,
			ObjectProvider<SegmentProcessor> segmentProcessorProvider,
			ObjectProvider<PaymentFileWriter> paymentFileWriterProvider, BatchDetailService batchDtlSvc,
			PaymentRequestRepository paymentReqRepo, LogUtility logUtility, JobRegistry jobRegistry,
			@Qualifier("applicationTaskExecutor") ThreadPoolTaskExecutor taskExecutor) {

		super(jobRepository, transactionManager, segmentReaderProvider, segmentProcessorProvider,
				paymentFileWriterProvider, logUtility);
		this.jobRepository = jobRepository;
		this.batchDtlSvc = batchDtlSvc;
		this.paymentReqRepo = paymentReqRepo;
		this.logUtility = logUtility;
		this.jobRegistry = jobRegistry;
		this.taskExecutor = taskExecutor;
	}

	@Override
	public String run(Object... args) {
		try {
			this.taskExecutor.initialize();
			TaskExecutorJobOperator jobOperator = new TaskExecutorJobOperator();
			jobOperator.setJobRepository(this.jobRepository);
			jobOperator.setTaskExecutor(this.taskExecutor);
			jobOperator.setJobRegistry(this.jobRegistry);
			jobOperator.afterPropertiesSet();

			String submittedJobs = this.batchDtlSvc.getCorrelatedBatch(BATCH_DETAIL_STATUS_INITIALIZE)
				.stream()
				.map(this::getCorrelatedPaymentDetail)
				.filter(Objects::nonNull)
				.map((cpr) -> this.submitJob(cpr, jobOperator))
				.collect(Collectors.joining(","));

			if (ObjectUtils.isEmpty(submittedJobs)) {
				this.logUtility.logMessage("No eligible correlation IDs found for processing.", INFO, log);
			}
			return submittedJobs;
		}
		catch (Exception ex) {
			this.logUtility.logMessage("Error while running Payment835Job: " + ex.getMessage(), ERROR, log);
			return null;
		}
	}

	private CorrelatedPaymentDetail getCorrelatedPaymentDetail(CorrelatedBatchDetail batch) {
		try {
			log.info("Processing batch with correlation ID: {}", batch.getCorrelationId());
			CorrelatedPaymentDetail cpr = this.paymentReqRepo.findCorrelatedPaymentRequest(batch.getCorrelationId(),
					Status.PI.name(), VirtualCardState.CREATED.name());
			// If payment count is not matching then raise alert if it exceeds the
			// threshold
			if (cpr == null || (!batch.getNumberOfPayments().equals(cpr.getNumberOfPayments()))) {
				this.paymentCountNotMatchAlert(batch, cpr);
				return null;
			}

			// If total amount is not matching then raise alert and update batch status
			if (!batch.getTotalAmount().equals(cpr.getTotalAmount())) {
				this.logUtility.logMessageWithPriority(
						AMOUNT_NOT_MATCH + batch.getCorrelationId() + ". expected total payment amount: "
								+ batch.getTotalAmount() + ", actual total payment amount: " + cpr.getTotalAmount(),
						ERROR, log, P3);
				this.batchDtlSvc.updateBatchDetailStatus(batch.getCorrelationId(), BATCH_DETAIL_STATUS_FAILED,
						BATCH_DETAIL_STATUS_INITIALIZE);
				return null;
			}

			// both are matching eligible for job submission
			return cpr;
		}
		catch (Exception ex) {
			log.error("Error while submitting job for correlation Id {}, Error: {}", batch.getCorrelationId(),
					ex.getMessage());
			return null;
		}
	}

	private String submitJob(CorrelatedPaymentDetail cpr, TaskExecutorJobOperator jobOperator) {
		String jobId = String.valueOf(System.currentTimeMillis());

		String processingCorrelationId = cpr.getCorrelationId();
		String jobName = JobName.PAYMENT_835_CREATOR_JOB.name() + "_" + processingCorrelationId;

		this.checkJobAlreadyRunningOrNot(this.jobRepository, jobName);

		this.batchDtlSvc.updateBatchDetailStatus(processingCorrelationId, BATCH_DETAIL_STATUS_PROGRESS,
				BATCH_DETAIL_STATUS_INITIALIZE);

		this.launchJob(jobOperator, jobId, super.buildJobParam(jobId, cpr),
				this.consolidatedFileJob(jobName, this.chunkSize));
		return jobId;

	}

	private void paymentCountNotMatchAlert(CorrelatedBatchDetail batch, CorrelatedPaymentDetail cpr) {
		String correlationId = batch.getCorrelationId();
		this.logUtility.logMessage(PAYMENT_COUNT_NOT_MATCH + correlationId, INFO, log);
		long differenceInHours = this.batchDtlSvc.timeDifferenceInHours(correlationId);
		if (differenceInHours >= this.alertTimeFor835File) {
			this.logUtility.logMessage(
					"This batch with correlation Id " + correlationId + " is in Pending status since "
							+ this.alertTimeFor835File + " hours hence failing the batch, ops review needed",
					INFO, log);
			this.logUtility.logMessageWithPriority(ALERT_MESSAGE + PAYMENT_COUNT_NOT_MATCH + correlationId
					+ ". expected payment count: " + batch.getNumberOfPayments() + ", actual payment count: "
					+ ((cpr != null) ? cpr.getNumberOfPayments() : "N/A"), ERROR, log, P3);
			this.batchDtlSvc.updateBatchDetailStatus(correlationId, BATCH_DETAIL_STATUS_FAILED,
					BATCH_DETAIL_STATUS_INITIALIZE);
		}
		else {
			this.logUtility.logMessage("Time difference is less than " + this.alertTimeFor835File + " hours and "
					+ PAYMENT_COUNT_NOT_MATCH + correlationId, INFO, log);
		}
	}

}
