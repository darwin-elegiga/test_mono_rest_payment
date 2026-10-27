package com.uhg.optum.financials.payments.payer.job;

import com.azure.storage.blob.BlobContainerClient;
import com.uhg.optum.financials.payments.payer.repository.FileTrackingRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import com.uhg.optum.financials.payments.payer.service.OutboundCorrelationResolver;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.SasUrlUtil;
import com.uhg.optum.financials.payments.payer.util.StorageFileUtil;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.batch.core.job.JobExecution;
import org.springframework.batch.core.launch.JobOperator;
import org.springframework.batch.core.listener.JobExecutionListener;
import org.springframework.batch.core.repository.JobRepository;
import org.springframework.batch.core.step.Step;
import org.springframework.transaction.PlatformTransactionManager;

import javax.sql.DataSource;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.BDDMockito.given;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.mock;

@ExtendWith(MockitoExtension.class)
class CompanionFileJobTests {

	@Mock
	private JobOperator jobOperator;

	@Mock
	private JobRepository jobRepository;

	@Mock
	private PlatformTransactionManager transactionManager;

	@Mock
	private LogUtility logUtility;

	@Mock
	private DataSource dataSource;

	@Mock
	private SasUrlUtil sasUrlUtil;

	@Mock
	private WebhookClient webhookClient;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@Mock
	private BlobContainerClient blobContainerClient;

	@Mock
	private BatchDetailService batchDetailService;

	@Mock
	private StorageFileUtil storageFileUtil;

	@Mock
	private FileTrackingRepository fileTrackingRepository;

	@Mock
	private OutboundCorrelationResolver outboundCorrelationResolver;

	private CompanionFileJob companionFileJob;

	@BeforeEach
	void setUp() throws Exception {
		this.companionFileJob = new CompanionFileJob(this.jobOperator, this.jobRepository, this.transactionManager,
				this.logUtility, this.dataSource, this.sasUrlUtil, this.webhookClient, this.paymentRequestRepository,
				this.blobContainerClient, this.batchDetailService, this.storageFileUtil, this.fileTrackingRepository,
				this.outboundCorrelationResolver);

		java.lang.reflect.Field chunkSizeField = CompanionFileJob.class.getDeclaredField("chunkSize");
		chunkSizeField.setAccessible(true);
		chunkSizeField.set(this.companionFileJob, 10);
	}

	@Test
	@DisplayName("Should run job and return jobId when eligibleCorrelationIds is empty")
	void shouldRunJobAndReturnJobIdWhenEligibleCorrelationIdsIsEmpty() {
		String jobId = this.companionFileJob.run();
		assertThat(jobId).isNotNull();
	}

	@Test
	@DisplayName("Should create companion processing step")
	void shouldCreateCompanionProcessingStep() {
		com.azure.storage.blob.BlobClient blobClient = mock(com.azure.storage.blob.BlobClient.class);
		com.azure.storage.blob.specialized.AppendBlobClient appendBlobClient = mock(
				com.azure.storage.blob.specialized.AppendBlobClient.class);
		given(this.blobContainerClient.getBlobClient(any())).willReturn(blobClient);
		given(blobClient.getAppendBlobClient()).willReturn(appendBlobClient);
		Step step = this.companionFileJob.companionProcessingStep("corrId", "corrId", "blobName");
		assertThat(step).isNotNull();
	}

	@Test
	@DisplayName("Should create finalize companion blob step")
	void shouldCreateFinalizeCompanionBlobStep() {
		Step step = this.companionFileJob.finalizeCompanionBlobStep("corrId", "corrId", "blobName");
		assertThat(step).isNotNull();
	}

	@Test
	@DisplayName("Should create job execution listener and log before/after job")
	void shouldCreateJobExecutionListenerAndLog() {
		JobExecutionListener listener = this.companionFileJob.jobExecutionListener();
		JobExecution jobExecution = mock(JobExecution.class);
		listener.beforeJob(jobExecution);
		listener.afterJob(jobExecution);
		assertThat(listener).isNotNull();
	}

}
