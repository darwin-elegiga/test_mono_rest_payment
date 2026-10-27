package com.uhg.optum.financials.payments.payer.job;

import com.azure.storage.blob.BlobContainerClient;
import com.azure.storage.blob.specialized.AppendBlobClient;
import com.uhg.optum.financials.payments.payer.constant.JobName;
import com.uhg.optum.financials.payments.payer.dto.CompanionDto;
import com.uhg.optum.financials.payments.payer.job.reader.CompanionDataReader;
import com.uhg.optum.financials.payments.payer.job.task.CompanionFileTask;
import com.uhg.optum.financials.payments.payer.job.task.PostCompanionCreationTask;
import com.uhg.optum.financials.payments.payer.job.writer.CompanionDataWriter;
import com.uhg.optum.financials.payments.payer.repository.FileTrackingRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import com.uhg.optum.financials.payments.payer.service.OutboundCorrelationResolver;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.util.CompanionFileUtil;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.SasUrlUtil;
import com.uhg.optum.financials.payments.payer.util.StorageFileUtil;
import lombok.extern.slf4j.Slf4j;
import org.springframework.batch.core.job.Job;
import org.springframework.batch.core.job.JobExecution;
import org.springframework.batch.core.job.builder.FlowBuilder;
import org.springframework.batch.core.job.builder.JobBuilder;
import org.springframework.batch.core.job.builder.SimpleJobBuilder;
import org.springframework.batch.core.job.flow.Flow;
import org.springframework.batch.core.job.parameters.JobParameters;
import org.springframework.batch.core.job.parameters.JobParametersBuilder;
import org.springframework.batch.core.launch.JobOperator;
import org.springframework.batch.core.listener.JobExecutionListener;
import org.springframework.batch.core.step.Step;
import org.springframework.batch.core.repository.JobRepository;
import org.springframework.batch.infrastructure.repeat.RepeatStatus;
import org.springframework.batch.core.step.builder.StepBuilder;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Configuration;
import org.springframework.transaction.PlatformTransactionManager;

import javax.sql.DataSource;
import java.util.List;
import java.util.concurrent.CopyOnWriteArrayList;

import static com.uhg.optum.financials.payments.payer.constant.DocumentConstants.JSON_EXTENSION;
import static com.uhg.optum.financials.payments.payer.constant.DocumentConstants.COMPANION_FILE;
import static com.uhg.optum.financials.payments.payer.constant.DocumentConstants.UNDERSCORE_DELIMITER;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.CLEANUP_DATA;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.JOB_ID;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;

@Slf4j
@Configuration
public class CompanionFileJob implements BaseJob {

	private final JobOperator jobOperator;

	private final JobRepository jobRepository;

	private final PlatformTransactionManager transactionManager;

	private final DataSource dataSource;

	private final SasUrlUtil sasUrlUtil;

	private final WebhookClient webhookClient;

	private final PaymentRequestRepository paymentRequestRepository;

	private final BlobContainerClient blobContainerClient;

	private final BatchDetailService batchDetailService;

	private final StorageFileUtil storageFileUtil;

	private final FileTrackingRepository fileTrackingRepository;

	private final OutboundCorrelationResolver outboundCorrelationResolver;

	@Value("${com.optum.job.chunkSize}")
	private int chunkSize;

	@Value("${endpoint.remittanceAdvice.process-c835}")
	private String remittanceAdvice835ProcessUrl;

	private final LogUtility logUtility;

	private final List<String> eligibleCorrelationIds;

	@Value("${endpoint.paymentActivityNotification}")
	private String paymentActivityNotificationEndpoint;

	@Value("${base.url}")
	private String baseUrl;

	public CompanionFileJob(JobOperator jobOperator, JobRepository jobRepository,
			PlatformTransactionManager transactionManager, LogUtility logUtility, DataSource dataSource,
			SasUrlUtil sasUrlUtil, WebhookClient webhookClient, PaymentRequestRepository paymentRequestRepository,
			@Qualifier("defaultBlobContainerClient") BlobContainerClient blobContainerClient,
			BatchDetailService batchDetailService, StorageFileUtil storageFileUtil,
			FileTrackingRepository fileTrackingRepository, OutboundCorrelationResolver outboundCorrelationResolver) {
		this.jobOperator = jobOperator;
		this.jobRepository = jobRepository;
		this.logUtility = logUtility;
		this.eligibleCorrelationIds = new CopyOnWriteArrayList<>();
		this.transactionManager = transactionManager;
		this.dataSource = dataSource;
		this.sasUrlUtil = sasUrlUtil;
		this.webhookClient = webhookClient;
		this.paymentRequestRepository = paymentRequestRepository;
		this.blobContainerClient = blobContainerClient;
		this.batchDetailService = batchDetailService;
		this.storageFileUtil = storageFileUtil;
		this.fileTrackingRepository = fileTrackingRepository;
		this.outboundCorrelationResolver = outboundCorrelationResolver;
	}

	@Override
	public String run(Object... args) {
		synchronized (this) {
			this.checkJobAlreadyRunningOrNot(this.jobRepository, JobName.COMPANION_FILE_JOB);
			// check for eligible correlationId's to be processed
			String jobId = String.valueOf(System.currentTimeMillis());
			JobParameters params = new JobParametersBuilder().addString(JOB_ID, jobId).toJobParameters();
			this.launchJob(this.jobOperator, jobId, params, this.companionFileJob());

			// Now that we have correlation IDs, build and run the processing job
			if (!this.eligibleCorrelationIds.isEmpty()) {
				this.launchJob(this.jobOperator, jobId, params, this.processingJob());
			}
			else {
				log.info("No eligible correlation IDs found to process");
			}

			return jobId;
		}
	}

	private Job companionFileJob() {
		JobBuilder jobBuilder = new JobBuilder("identifyCorrelationIdsJob", this.jobRepository);
		SimpleJobBuilder flowBuilder = jobBuilder.start(identifyEligibleCorrelationIds());

		return flowBuilder.listener(jobExecutionListener()).build();
	}

	private Step identifyEligibleCorrelationIds() {
		return new StepBuilder("identifyEligibleCorrelationIds", this.jobRepository)
			.tasklet(new CompanionFileTask(this.paymentRequestRepository, this.eligibleCorrelationIds),
					this.transactionManager)
			.build();
	}

	private Job processingJob() {

		// Build job with steps for each correlation ID
		JobBuilder jobBuilder = new JobBuilder("companionJob", this.jobRepository);

		// Create a flow with all the dynamic steps
		Flow flow = createProcessingFlow();

		return jobBuilder.start(flow).end().listener(jobExecutionListener()).build();
	}

	private Flow createProcessingFlow() {
		FlowBuilder<Flow> flowBuilder = new FlowBuilder<>("processingFlow");

		if (this.eligibleCorrelationIds.isEmpty()) {
			// Empty flow if no correlationIds
			return flowBuilder.end();
		}

		// First correlation ID handled differently to start the flow
		String correlationId = this.eligibleCorrelationIds.getFirst();
		String outboundCorrelationId = this.outboundCorrelationResolver.resolveActualCorrelationId(correlationId);
		String companionBlobName = this.getCompanionBlobName();
		flowBuilder = flowBuilder
			.start(companionProcessingStep(correlationId, outboundCorrelationId, companionBlobName))
			.next(finalizeCompanionBlobStep(correlationId, outboundCorrelationId, companionBlobName))
			.next(postCompanionCreationTask(correlationId, companionBlobName))
			.next(cleanUpDataForCorrelationIds(correlationId));

		// Add remaining correlation IDs
		for (int i = 1; i < this.eligibleCorrelationIds.size(); i++) {
			correlationId = this.eligibleCorrelationIds.get(i);
			outboundCorrelationId = this.outboundCorrelationResolver.resolveActualCorrelationId(correlationId);
			companionBlobName = this.getCompanionBlobName();
			flowBuilder = flowBuilder
				.next(companionProcessingStep(correlationId, outboundCorrelationId, companionBlobName))
				.next(finalizeCompanionBlobStep(correlationId, outboundCorrelationId, companionBlobName))
				.next(postCompanionCreationTask(correlationId, companionBlobName))
				.next(cleanUpDataForCorrelationIds(correlationId));
		}

		return flowBuilder.build();
	}

	private Step postCompanionCreationTask(String correlationId, String companionBlobName) {
		return new StepBuilder("postCompanionCreation_" + correlationId, this.jobRepository)
			.tasklet(createPostCompanionCreationTask(correlationId, companionBlobName), this.transactionManager)
			.build();
	}

	private PostCompanionCreationTask createPostCompanionCreationTask(String correlationId, String companionBlobName) {
		return PostCompanionCreationTask.builder()
			.correlationId(correlationId)
			.companionBlobPath(companionBlobName)
			.fileTrackingRepository(this.fileTrackingRepository)
			.paymentRequestRepository(this.paymentRequestRepository)
			.remittanceAdvice835ProcessUrl(this.remittanceAdvice835ProcessUrl)
			.paymentActivityNotificationEndpoint(this.paymentActivityNotificationEndpoint)
			.sasUrlUtil(this.sasUrlUtil)
			.logUtility(this.logUtility)
			.webhookClient(this.webhookClient)
			.storageFileUtil(this.storageFileUtil)
			.batchDetailService(this.batchDetailService)
			.build();

	}

	public Step companionProcessingStep(String processingCorrelationId, String outboundCorrelationId,
			String companionBlobName) {
		return new StepBuilder("createCompanion_" + processingCorrelationId, this.jobRepository)
			.<CompanionDto, CompanionDto>chunk(this.chunkSize)
			.reader(new CompanionDataReader(processingCorrelationId, this.dataSource, this.baseUrl))
			.writer(createCompanionDataWriter(outboundCorrelationId, companionBlobName, true))
			.transactionManager(this.transactionManager)
			.build();
	}

	public Step finalizeCompanionBlobStep(String processingCorrelationId, String outboundCorrelationId,
			String companionBlobName) {
		return new StepBuilder("finalizeCompanion_" + processingCorrelationId, this.jobRepository)
			.tasklet((contribution, chunkContext) -> {
				CompanionDataWriter writer = createCompanionDataWriter(outboundCorrelationId, companionBlobName, false);
				writer.finalizeBlob();
				return RepeatStatus.FINISHED;
			}, this.transactionManager)
			.build();
	}

	private CompanionDataWriter createCompanionDataWriter(String correlationId, String companionBlobName,
			boolean initializeBlob) {
		AppendBlobClient appendBlobClient = this.blobContainerClient.getBlobClient(companionBlobName)
			.getAppendBlobClient();
		return new CompanionDataWriter(appendBlobClient, correlationId, initializeBlob);
	}

	// Step to clean up data for correlation IDs
	private Step cleanUpDataForCorrelationIds(String correlationId) {
		return new StepBuilder(CLEANUP_DATA, this.jobRepository).tasklet((contribution, chunkContext) -> {
			this.logUtility.logMessage("Clean up data.", INFO, log);
			return RepeatStatus.FINISHED;
		}, this.transactionManager).build();

	}

	public JobExecutionListener jobExecutionListener() {
		return new JobExecutionListener() {
			@Override
			public void beforeJob(JobExecution jobExecution) {
				log.info("Job starting...");
			}

			@Override
			public void afterJob(JobExecution jobExecution) {
				log.info("Job finished with status: {}", jobExecution.getStatus());
			}
		};
	}

	private String getCompanionBlobName() {
		return CompanionFileUtil.getCurrentDateTimeWithMillis() + UNDERSCORE_DELIMITER + COMPANION_FILE
				+ JSON_EXTENSION;
	}

}
