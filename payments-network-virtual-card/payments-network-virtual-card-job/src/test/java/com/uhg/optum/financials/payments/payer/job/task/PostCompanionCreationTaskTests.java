package com.uhg.optum.financials.payments.payer.job.task;

import com.uhg.optum.financials.payments.payer.model.FileTracking;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.repository.FileTrackingRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.SasUrlUtil;
import com.uhg.optum.financials.payments.payer.util.StorageFileUtil;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.batch.core.scope.context.ChunkContext;
import org.springframework.batch.core.step.StepContribution;
import org.springframework.batch.infrastructure.repeat.RepeatStatus;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.Collections;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

@ExtendWith(MockitoExtension.class)
class PostCompanionCreationTaskTests {

	private static final String CORRELATION_ID = "corr-123";

	private static final String COMPANION_BLOB_PATH = "processed/companion/corr-123.json";

	private static final String OUTBOUND_835_PATH = "outbound/835/corr-123.txt";

	private static final String REMITTANCE_URL = "https://remittance";

	private static final String PAYMENT_ACTIVITY_URL = "https://payment-activity";

	private PostCompanionCreationTask task;

	@Mock
	private FileTrackingRepository fileTrackingRepository;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@Mock
	private SasUrlUtil sasUrlUtil;

	@Mock
	private LogUtility logUtility;

	@Mock
	private WebhookClient webhookClient;

	@Mock
	private StorageFileUtil storageFileUtil;

	@Mock
	private BatchDetailService batchDetailService;

	@Mock
	private StepContribution contribution;

	@Mock
	private ChunkContext chunkContext;

	@BeforeEach
	void setUp() {
		this.task = PostCompanionCreationTask.builder()
			.correlationId(CORRELATION_ID)
			.companionBlobPath(COMPANION_BLOB_PATH)
			.fileTrackingRepository(this.fileTrackingRepository)
			.paymentRequestRepository(this.paymentRequestRepository)
			.remittanceAdvice835ProcessUrl(REMITTANCE_URL)
			.sasUrlUtil(this.sasUrlUtil)
			.logUtility(this.logUtility)
			.webhookClient(this.webhookClient)
			.storageFileUtil(this.storageFileUtil)
			.batchDetailService(this.batchDetailService)
			.paymentActivityNotificationEndpoint(PAYMENT_ACTIVITY_URL)
			.build();
	}

	@Test
	void execute_shouldMarkCompanionStatusFailedWhenRemittanceFails() {
		FileTracking fileTracking = new FileTracking();
		fileTracking.setCorrelationId(CORRELATION_ID);
		fileTracking.set_835(OUTBOUND_835_PATH);

		Mockito.when(this.fileTrackingRepository.findByCorrelationId(CORRELATION_ID))
			.thenReturn(Optional.of(fileTracking));
		Mockito.when(this.paymentRequestRepository.findOrgByCorrelationId(CORRELATION_ID)).thenReturn("ORG");
		Mockito.when(this.sasUrlUtil.generateSasUrl(OUTBOUND_835_PATH, "30")).thenReturn("https://835");
		Mockito.when(this.sasUrlUtil.generateSasUrl(COMPANION_BLOB_PATH, "30")).thenReturn("https://companion");
		Mockito.doThrow(new RuntimeException("remittance failed"))
			.when(this.webhookClient)
			.notifyRemittanceClient(Mockito.any(), Mockito.eq(REMITTANCE_URL));

		assertThatThrownBy(() -> this.task.execute(this.contribution, this.chunkContext))
			.isInstanceOf(RuntimeException.class)
			.hasMessage("remittance failed");

		Mockito.verify(this.fileTrackingRepository)
			.updateCompanionAndStatusByCorrelationId(CORRELATION_ID, COMPANION_BLOB_PATH, "FAILED");
		Mockito.verify(this.fileTrackingRepository, Mockito.never())
			.updateCompanionStatusByCorrelationId(CORRELATION_ID, "FAILED");
		Mockito.verify(this.fileTrackingRepository, Mockito.never())
			.updatePaymentActivityStatusByCorrelationId(Mockito.anyString(), Mockito.anyString());
	}

	@Test
	void execute_shouldMarkCompanionStatusFailedWhenCompanionUpdateFails() {
		FileTracking fileTracking = new FileTracking();
		fileTracking.setCorrelationId(CORRELATION_ID);
		fileTracking.set_835(OUTBOUND_835_PATH);

		Mockito.when(this.fileTrackingRepository.findByCorrelationId(CORRELATION_ID))
			.thenReturn(Optional.of(fileTracking));
		Mockito.when(this.paymentRequestRepository.findOrgByCorrelationId(CORRELATION_ID)).thenReturn("ORG");
		Mockito.when(this.sasUrlUtil.generateSasUrl(OUTBOUND_835_PATH, "30")).thenReturn("https://835");
		Mockito.when(this.sasUrlUtil.generateSasUrl(COMPANION_BLOB_PATH, "30")).thenReturn("https://companion");
		Mockito.when(this.webhookClient.notifyRemittanceClient(Mockito.any(), Mockito.eq(REMITTANCE_URL)))
			.thenReturn(true);
		Mockito.doThrow(new RuntimeException("companion update failed"))
			.when(this.fileTrackingRepository)
			.updateCompanionAndStatusByCorrelationId(CORRELATION_ID, COMPANION_BLOB_PATH, "SUCCESS");

		assertThatThrownBy(() -> this.task.execute(this.contribution, this.chunkContext))
			.isInstanceOf(RuntimeException.class)
			.hasMessage("companion update failed");

		Mockito.verify(this.fileTrackingRepository)
			.updateCompanionAndStatusByCorrelationId(CORRELATION_ID, COMPANION_BLOB_PATH, "FAILED");
		Mockito.verify(this.fileTrackingRepository, Mockito.never())
			.updateCompanionStatusByCorrelationId(CORRELATION_ID, "SUCCESS");
		Mockito.verify(this.fileTrackingRepository, Mockito.never())
			.updateCompanionStatusByCorrelationId(CORRELATION_ID, "FAILED");
		Mockito.verify(this.fileTrackingRepository, Mockito.never())
			.updatePaymentActivityStatusByCorrelationId(Mockito.anyString(), Mockito.anyString());
	}

	@Test
	void execute_shouldMarkCompanionStatusFailedWhenNoFileTrackingRowIsUpdated() {
		FileTracking fileTracking = new FileTracking();
		fileTracking.setCorrelationId(CORRELATION_ID);
		fileTracking.set_835(OUTBOUND_835_PATH);

		Mockito.when(this.fileTrackingRepository.findByCorrelationId(CORRELATION_ID))
			.thenReturn(Optional.of(fileTracking));
		Mockito.when(this.paymentRequestRepository.findOrgByCorrelationId(CORRELATION_ID)).thenReturn("ORG");
		Mockito.when(this.sasUrlUtil.generateSasUrl(OUTBOUND_835_PATH, "30")).thenReturn("https://835");
		Mockito.when(this.sasUrlUtil.generateSasUrl(COMPANION_BLOB_PATH, "30")).thenReturn("https://companion");
		Mockito.when(this.webhookClient.notifyRemittanceClient(Mockito.any(), Mockito.eq(REMITTANCE_URL)))
			.thenReturn(true);
		Mockito
			.when(this.fileTrackingRepository.updateCompanionAndStatusByCorrelationId(CORRELATION_ID,
					COMPANION_BLOB_PATH, "SUCCESS"))
			.thenReturn(0);

		assertThatThrownBy(() -> this.task.execute(this.contribution, this.chunkContext))
			.isInstanceOf(IllegalStateException.class)
			.hasMessage("No file tracking row updated for correlationId: " + CORRELATION_ID);

		Mockito.verify(this.fileTrackingRepository)
			.updateCompanionAndStatusByCorrelationId(CORRELATION_ID, COMPANION_BLOB_PATH, "FAILED");
		Mockito.verify(this.fileTrackingRepository, Mockito.never())
			.updateCompanionStatusByCorrelationId(CORRELATION_ID, "FAILED");
		Mockito.verify(this.fileTrackingRepository, Mockito.never())
			.updatePaymentActivityStatusByCorrelationId(Mockito.anyString(), Mockito.anyString());
	}

	@Test
	void execute_shouldFallbackToStatusOnlyFailedUpdateWhenCompanionPathIsBlank() {
		PostCompanionCreationTask blankPathTask = PostCompanionCreationTask.builder()
			.correlationId(CORRELATION_ID)
			.companionBlobPath("   ")
			.fileTrackingRepository(this.fileTrackingRepository)
			.paymentRequestRepository(this.paymentRequestRepository)
			.remittanceAdvice835ProcessUrl(REMITTANCE_URL)
			.sasUrlUtil(this.sasUrlUtil)
			.logUtility(this.logUtility)
			.webhookClient(this.webhookClient)
			.storageFileUtil(this.storageFileUtil)
			.batchDetailService(this.batchDetailService)
			.paymentActivityNotificationEndpoint(PAYMENT_ACTIVITY_URL)
			.build();

		FileTracking fileTracking = new FileTracking();
		fileTracking.setCorrelationId(CORRELATION_ID);
		fileTracking.set_835(OUTBOUND_835_PATH);

		Mockito.when(this.fileTrackingRepository.findByCorrelationId(CORRELATION_ID))
			.thenReturn(Optional.of(fileTracking));
		Mockito.when(this.paymentRequestRepository.findOrgByCorrelationId(CORRELATION_ID)).thenReturn("ORG");
		Mockito.when(this.sasUrlUtil.generateSasUrl(OUTBOUND_835_PATH, "30")).thenReturn("https://835");
		Mockito.when(this.sasUrlUtil.generateSasUrl("   ", "30")).thenReturn("https://companion");
		Mockito.doThrow(new RuntimeException("remittance failed"))
			.when(this.webhookClient)
			.notifyRemittanceClient(Mockito.any(), Mockito.eq(REMITTANCE_URL));

		assertThatThrownBy(() -> blankPathTask.execute(this.contribution, this.chunkContext))
			.isInstanceOf(RuntimeException.class)
			.hasMessage("remittance failed");

		Mockito.verify(this.fileTrackingRepository).updateCompanionStatusByCorrelationId(CORRELATION_ID, "FAILED");
		Mockito.verify(this.fileTrackingRepository, Mockito.never())
			.updateCompanionAndStatusByCorrelationId(CORRELATION_ID, "   ", "FAILED");
		Mockito.verify(this.fileTrackingRepository, Mockito.never())
			.updatePaymentActivityStatusByCorrelationId(Mockito.anyString(), Mockito.anyString());
	}

	@Test
	void execute_shouldMarkCompanionStatusSuccessWhenRemittanceAndUpdateSucceed() throws Exception {
		FileTracking fileTracking = new FileTracking();
		fileTracking.setCorrelationId(CORRELATION_ID);
		fileTracking.set_835(OUTBOUND_835_PATH);

		Mockito.when(this.fileTrackingRepository.findByCorrelationId(CORRELATION_ID))
			.thenReturn(Optional.of(fileTracking));
		Mockito.when(this.paymentRequestRepository.findOrgByCorrelationId(CORRELATION_ID)).thenReturn("ORG");
		Mockito.when(this.paymentRequestRepository.findByCorrelationId(CORRELATION_ID))
			.thenReturn(Collections.emptyList());
		Mockito.when(this.sasUrlUtil.generateSasUrl(OUTBOUND_835_PATH, "30")).thenReturn("https://835");
		Mockito.when(this.sasUrlUtil.generateSasUrl(COMPANION_BLOB_PATH, "30")).thenReturn("https://companion");
		Mockito.when(this.webhookClient.notifyRemittanceClient(Mockito.any(), Mockito.eq(REMITTANCE_URL)))
			.thenReturn(true);
		Mockito
			.when(this.fileTrackingRepository.updateCompanionAndStatusByCorrelationId(CORRELATION_ID,
					COMPANION_BLOB_PATH, "SUCCESS"))
			.thenReturn(1);

		RepeatStatus result = this.task.execute(this.contribution, this.chunkContext);

		assertThat(result).isEqualTo(RepeatStatus.FINISHED);
		Mockito.verify(this.fileTrackingRepository)
			.updateCompanionAndStatusByCorrelationId(CORRELATION_ID, COMPANION_BLOB_PATH, "SUCCESS");
		Mockito.verify(this.batchDetailService).updateBatchDetailStatus(CORRELATION_ID, "C", "R");
		Mockito.verify(this.fileTrackingRepository, Mockito.never())
			.updateCompanionStatusByCorrelationId(CORRELATION_ID, "SUCCESS");
		Mockito.verify(this.fileTrackingRepository)
			.updatePaymentActivityStatusByCorrelationId(CORRELATION_ID, "SUCCESS");
	}

	@Test
	void execute_shouldMarkCompanionStatusFailedWhenRemittanceReturnsFalse() {
		FileTracking fileTracking = new FileTracking();
		fileTracking.setCorrelationId(CORRELATION_ID);
		fileTracking.set_835(OUTBOUND_835_PATH);

		Mockito.when(this.fileTrackingRepository.findByCorrelationId(CORRELATION_ID))
			.thenReturn(Optional.of(fileTracking));
		Mockito.when(this.paymentRequestRepository.findOrgByCorrelationId(CORRELATION_ID)).thenReturn("ORG");
		Mockito.when(this.sasUrlUtil.generateSasUrl(OUTBOUND_835_PATH, "30")).thenReturn("https://835");
		Mockito.when(this.sasUrlUtil.generateSasUrl(COMPANION_BLOB_PATH, "30")).thenReturn("https://companion");
		Mockito.when(this.webhookClient.notifyRemittanceClient(Mockito.any(), Mockito.eq(REMITTANCE_URL)))
			.thenReturn(false);

		assertThatThrownBy(() -> this.task.execute(this.contribution, this.chunkContext))
			.isInstanceOf(IllegalStateException.class)
			.hasMessageContaining("Remittance webhook call failed for correlationId: " + CORRELATION_ID);

		Mockito.verify(this.fileTrackingRepository)
			.updateCompanionAndStatusByCorrelationId(CORRELATION_ID, COMPANION_BLOB_PATH, "FAILED");
		Mockito.verify(this.fileTrackingRepository, Mockito.never())
			.updatePaymentActivityStatusByCorrelationId(Mockito.anyString(), Mockito.anyString());
	}

	@Test
	void execute_shouldKeepBatchAsCompanionCreatedWhenPaymentActivityFails() throws Exception {
		FileTracking fileTracking = new FileTracking();
		fileTracking.setCorrelationId(CORRELATION_ID);
		fileTracking.set_835(OUTBOUND_835_PATH);

		PaymentRequest paymentRequest = Mockito.mock(PaymentRequest.class);
		PaymentDetails paymentDetails = Mockito.mock(PaymentDetails.class);
		Mockito.when(paymentRequest.getPaymentDetails()).thenReturn(paymentDetails);
		Mockito.when(paymentDetails.getId()).thenReturn("pid-123");
		Mockito.when(paymentDetails.getNetworkPaymentId()).thenReturn("npid-123");
		Mockito.when(paymentDetails.getAmount()).thenReturn(BigDecimal.TEN);
		Mockito.when(paymentRequest.getCreatedOn()).thenReturn(LocalDateTime.now());
		Mockito.when(paymentRequest.getOrgShortCode()).thenReturn("ORG");
		Mockito.when(paymentRequest.getMetadata()).thenReturn(Collections.emptyMap());

		Mockito.when(this.fileTrackingRepository.findByCorrelationId(CORRELATION_ID))
			.thenReturn(Optional.of(fileTracking));
		Mockito.when(this.paymentRequestRepository.findOrgByCorrelationId(CORRELATION_ID)).thenReturn("ORG");
		Mockito.when(this.paymentRequestRepository.findByCorrelationId(CORRELATION_ID))
			.thenReturn(Collections.singletonList(paymentRequest));
		Mockito.when(this.sasUrlUtil.generateSasUrl(OUTBOUND_835_PATH, "30")).thenReturn("https://835");
		Mockito.when(this.sasUrlUtil.generateSasUrl(COMPANION_BLOB_PATH, "30")).thenReturn("https://companion");
		Mockito.when(this.webhookClient.notifyRemittanceClient(Mockito.any(), Mockito.eq(REMITTANCE_URL)))
			.thenReturn(true);
		Mockito
			.when(this.fileTrackingRepository.updateCompanionAndStatusByCorrelationId(CORRELATION_ID,
					COMPANION_BLOB_PATH, "SUCCESS"))
			.thenReturn(1);
		Mockito
			.when(this.webhookClient.notifyClientWithStatus(Mockito.any(), Mockito.eq("ORG"),
					Mockito.eq(PAYMENT_ACTIVITY_URL)))
			.thenReturn(false);

		RepeatStatus result = this.task.execute(this.contribution, this.chunkContext);

		assertThat(result).isEqualTo(RepeatStatus.FINISHED);
		Mockito.verify(this.fileTrackingRepository)
			.updatePaymentActivityStatusByCorrelationId(CORRELATION_ID, "FAILED");
		Mockito.verify(this.batchDetailService).updateBatchDetailStatus(CORRELATION_ID, "C", "R");
	}

}
