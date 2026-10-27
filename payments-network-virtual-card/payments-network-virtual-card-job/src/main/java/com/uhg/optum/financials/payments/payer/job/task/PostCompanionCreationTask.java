package com.uhg.optum.financials.payments.payer.job.task;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.dto.Consolidated835FileDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.PaymentActivityWebhookRequestDto;
import com.uhg.optum.financials.payments.payer.model.FileTracking;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.repository.FileTrackingRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.util.StorageFileUtil;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.SasUrlUtil;
import com.uhg.optum.financials.payments.payer.util.WebhookRequestUtil;
import lombok.Builder;
import lombok.extern.slf4j.Slf4j;
import org.springframework.batch.core.scope.context.ChunkContext;
import org.springframework.batch.core.step.StepContribution;
import org.springframework.batch.core.step.tasklet.Tasklet;
import org.springframework.batch.infrastructure.repeat.RepeatStatus;

import java.net.URLEncoder;
import java.nio.charset.StandardCharsets;
import java.util.List;
import java.util.Optional;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BATCH_DETAIL_STATUS_835_CREATED;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BATCH_DETAIL_STATUS_COMPANION_CREATED;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_CONVERTING_DTO_TO_JSON;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.ACTIVITY;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.DOT_DELIMITER;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.PAYMENT;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.DEBUG;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;

@Slf4j
@Builder
public class PostCompanionCreationTask implements Tasklet {

	private static final String STORAGE_URL_TIME_TO_LIVE = "30"; // 30 minutes

	private static final String SLASH_DELIMITER = "/";

	private static final String STATUS_SUCCESS = "SUCCESS";

	private static final String STATUS_FAILED = "FAILED";

	private final String correlationId;

	private final String companionBlobPath;

	private final FileTrackingRepository fileTrackingRepository;

	private final PaymentRequestRepository paymentRequestRepository;

	private final String remittanceAdvice835ProcessUrl;

	private final SasUrlUtil sasUrlUtil;

	private final LogUtility logUtility;

	private final WebhookClient webhookClient;

	private final StorageFileUtil storageFileUtil;

	private final BatchDetailService batchDetailService;

	private final String paymentActivityNotificationEndpoint;

	@Override
	public RepeatStatus execute(StepContribution contribution, ChunkContext chunkContext) throws Exception {

		// Perform post-companion creation tasks here
		this.logUtility.logMessage("PostCompanionCreationTask started for correlationId: " + this.correlationId, DEBUG,
				log);

		Optional<FileTracking> fileTracking = this.fileTrackingRepository.findByCorrelationId(this.correlationId);
		this.logUtility.logMessage("Finding file tracking repos for correlatinoId: " + this.correlationId, INFO, log);

		if (fileTracking.isPresent()) {
			FileTracking fileInfo = fileTracking.get();
			String outboundFilePath = fileInfo.get_835();
			this.logUtility.logMessage("Calling remittance with outbound file path: " + outboundFilePath, INFO, log);
			try {
				callRemittance(outboundFilePath);
				int updatedRows = this.fileTrackingRepository.updateCompanionAndStatusByCorrelationId(
						this.correlationId, this.companionBlobPath, STATUS_SUCCESS);
				if (updatedRows == 0) {
					throw new IllegalStateException(
							"No file tracking row updated for correlationId: " + this.correlationId);
				}
				this.logUtility.logMessage("Updated companion path and status in file tracking table", INFO, log);
				this.batchDetailService.updateBatchDetailStatus(this.correlationId,
						BATCH_DETAIL_STATUS_COMPANION_CREATED, BATCH_DETAIL_STATUS_835_CREATED);
			}
			catch (Exception ex) {
				try {
					if (this.companionBlobPath != null && !this.companionBlobPath.isBlank()) {
						this.fileTrackingRepository.updateCompanionAndStatusByCorrelationId(this.correlationId,
								this.companionBlobPath, STATUS_FAILED);
					}
					else {
						this.fileTrackingRepository.updateCompanionStatusByCorrelationId(this.correlationId,
								STATUS_FAILED);
					}
				}
				catch (Exception statusUpdateEx) {
					this.logUtility.logMessage("Failed to update companion status to FAILED for correlationId: "
							+ this.correlationId + " due to: " + statusUpdateEx.getMessage(), ERROR, log);
					ex.addSuppressed(statusUpdateEx);
				}
				throw ex;
			}
		}

		List<PaymentRequest> paymentRequestList = this.paymentRequestRepository.findByCorrelationId(this.correlationId);
		boolean paymentActivityTriggerSuccess = true;
		for (PaymentRequest paymentRequest : paymentRequestList) {
			PaymentActivityWebhookRequestDto paymentActivityWebhookRequestDto = (PaymentActivityWebhookRequestDto) WebhookRequestUtil
				.getPaymentActivityWebhookBaseDto(PAYMENT + DOT_DELIMITER + ACTIVITY,
						paymentRequest.getPaymentDetails().getId(), paymentRequest.getMetadata(), null, null,
						paymentRequest.getPaymentDetails().getNetworkPaymentId());
			paymentActivityWebhookRequestDto.setData(WebhookRequestUtil.getPaymentActivityWebhookRequestPayload(
					paymentRequest.getCreatedOn(), paymentRequest.getPaymentDetails().getAmount()));
			log.debug("Sending Payment Activity Event to Issuing :{}", paymentActivityWebhookRequestDto);
			ObjectMapper mapper = new ObjectMapper();
			try {
				String sentpaymentActivityDtoJson = mapper.writerWithDefaultPrettyPrinter()
					.writeValueAsString(paymentActivityWebhookRequestDto);
				this.logUtility.logMessage("Sending payment Activity webhook payload: " + sentpaymentActivityDtoJson,
						AppConstants.INFO, log);
			}
			catch (JsonProcessingException ex) {
				this.logUtility.logMessage(ERROR_CONVERTING_DTO_TO_JSON + ex.getMessage(), ERROR, log);
			}
			boolean paymentActivitySent = this.webhookClient.notifyClientWithStatus(paymentActivityWebhookRequestDto,
					paymentRequest.getOrgShortCode(), this.paymentActivityNotificationEndpoint);
			if (!paymentActivitySent) {
				paymentActivityTriggerSuccess = false;
				this.logUtility.logMessageWithPriority(
						"Payment activity trigger failed for paymentId: " + paymentRequest.getPaymentDetails().getId(),
						ERROR, log, P3);
			}
		}
		this.fileTrackingRepository.updatePaymentActivityStatusByCorrelationId(this.correlationId,
				paymentActivityTriggerSuccess ? STATUS_SUCCESS : STATUS_FAILED);
		this.logUtility.logMessage("Post companion job completed: ", INFO, log);
		return RepeatStatus.FINISHED;
	}

	private void callRemittance(String outboundFilePath) {
		this.logUtility.logMessage("Webhook call to remittance started for correlationId: " + this.correlationId, INFO,
				log);

		String consolidated835FileSasUrl = this.sasUrlUtil.generateSasUrl(outboundFilePath, STORAGE_URL_TIME_TO_LIVE);
		String encoded835FileSasUrl = URLEncoder.encode(consolidated835FileSasUrl, StandardCharsets.UTF_8);
		this.logUtility.logMessage("SAS URL generated for consolidated 835 file: " + consolidated835FileSasUrl, DEBUG,
				log);
		String companionFileSasUrl = this.sasUrlUtil.generateSasUrl(this.companionBlobPath, STORAGE_URL_TIME_TO_LIVE);
		String encodedCompanionFileSasUrl = URLEncoder.encode(companionFileSasUrl, StandardCharsets.UTF_8);
		this.logUtility.logMessage("SAS URL generated for companion file: " + companionFileSasUrl, DEBUG, log);

		// Webhook call to remittance
		String orgShortCode = this.paymentRequestRepository.findOrgByCorrelationId(this.correlationId);

		Consolidated835FileDto consolidated835FileDto = WebhookRequestUtil.getRemittanceWebhook(encoded835FileSasUrl,
				encodedCompanionFileSasUrl, orgShortCode);
		boolean remittanceSuccess = this.webhookClient.notifyRemittanceClient(consolidated835FileDto,
				this.remittanceAdvice835ProcessUrl);
		if (!remittanceSuccess) {
			this.logUtility.logMessageWithPriority(
					"Remittance webhook call failed for correlationId: " + this.correlationId, ERROR, log, P3);
			throw new IllegalStateException("Remittance webhook call failed for correlationId: " + this.correlationId);
		}
		this.logUtility.logMessage("Webhook call to remittance completed for correlationId: " + this.correlationId,
				INFO, log);
	}

}
