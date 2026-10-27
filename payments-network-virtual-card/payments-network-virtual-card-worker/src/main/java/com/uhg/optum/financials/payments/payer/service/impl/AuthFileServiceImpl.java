package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobContainerClient;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.dto.authevent.MerchantInfo;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookBaseDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookPayload;
import com.uhg.optum.financials.payments.payer.exception.InvalidAuthFileException;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.AuthFileService;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.util.AmountConverter;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.FileProcessingStatusLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.ModelConversionMapper;
import com.uhg.optum.financials.payments.payer.util.WebhookRequestUtil;
import com.uhg.optum.financials.shared.core.storage.AzureStorageUtil;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.csv.CSVFormat;
import org.apache.commons.csv.CSVParser;
import org.apache.commons.csv.CSVRecord;
import org.apache.commons.io.input.BOMInputStream;
import org.apache.commons.lang3.StringUtils;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.math.BigDecimal;
import java.math.RoundingMode;
import java.nio.charset.StandardCharsets;
import java.time.LocalDateTime;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import java.util.stream.Collectors;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.AUTH_FILE_EXTRACT_PROCESSOR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_CONVERTING_DTO_TO_JSON;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.NO_VALID_RECORDS_MSG_TEMPLATE;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.WARN;
import static com.uhg.optum.financials.payments.payer.constant.Constants.APPROVED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.AUTH;
import static com.uhg.optum.financials.payments.payer.constant.Constants.DOT_DELIMITER;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;

@Service
@Slf4j
public class AuthFileServiceImpl implements AuthFileService {

	private final AuthExtractRepository authExtractRepository;

	private final VirtualCardsRepository virtualCardsRepository;

	private final BlobContainerClient blobContainerClient;

	private final LogUtility logUtility;

	private final WebhookClient webhookClient;

	private final VirtualCardBalanceService virtualCardBalanceService;

	@Value("${endpoint.issuing.cardEventNotification}")
	private String cardAuthApprovedNotificationEndpoint;

	private final AuditLogger auditLogger;

	private final PaymentRequestRepository paymentRequestRepository;

	private final ModelConversionMapper cardDetailsMapper;

	private final FileProcessingStatusLogger fileProcessingStatusLogger;

	ObjectMapper mapper = new ObjectMapper();

	private static final String RECEIPT_ID = "receipt_id";

	public AuthFileServiceImpl(AuthExtractRepository authExtractRepository,
			VirtualCardsRepository virtualCardsRepository, BlobContainerClient blobContainerClient,
			LogUtility logUtility, WebhookClient webhookClient, VirtualCardBalanceService virtualCardBalanceService,
			AuditLogger auditLogger, PaymentRequestRepository paymentRequestRepository,
			ModelConversionMapper cardDetailsMapper, FileProcessingStatusLogger fileProcessingStatusLogger) {
		this.authExtractRepository = authExtractRepository;
		this.virtualCardsRepository = virtualCardsRepository;
		this.blobContainerClient = blobContainerClient;
		this.logUtility = logUtility;
		this.webhookClient = webhookClient;
		this.virtualCardBalanceService = virtualCardBalanceService;
		this.auditLogger = auditLogger;
		this.paymentRequestRepository = paymentRequestRepository;
		this.cardDetailsMapper = cardDetailsMapper;
		this.fileProcessingStatusLogger = fileProcessingStatusLogger;
	}

	@Override
	public void processAuthFile(String filePath) {
		this.logUtility.logMessage("Processing Auth file at: " + filePath, INFO, log);
		List<AuthExtract> authEventExtracts = new ArrayList<>();
		Map<String, String> errorMap = new HashMap<>();
		int totalRecordCount = 0;
		try (InputStream ins = AzureStorageUtil.readBlobItemAsStream(this.blobContainerClient, filePath)) {
			BufferedReader reader = new BufferedReader(
					new InputStreamReader(BOMInputStream.builder().setInputStream(ins).get(), StandardCharsets.UTF_8));

			String headerLine = reader.readLine();
			String[] headers = headerLine.split(",");

			CSVFormat format = CSVFormat.DEFAULT.builder()
				.setHeader(headers)
				.setSkipHeaderRecord(false)
				.setDelimiter(',')
				.setIgnoreHeaderCase(true)
				.setTrim(true)
				.build();

			CSVParser csvParser = new CSVParser(reader, format);
			List<CSVRecord> csvRecords = csvParser.getRecords();
			totalRecordCount = csvRecords.size();

			authEventExtracts = csvRecords.stream()
				.filter((csvRecord) -> csvRecord.stream().anyMatch((value) -> value != null && !value.isEmpty()))
				.map((record) -> mapRecordToEntity(record, errorMap))
				.filter(Objects::nonNull) // filter out failed mappings
				.toList();

			List<AuthExtract> invalidExtracts = authEventExtracts.stream()
				.filter((extract) -> extract.getCardId() == null
						|| !this.virtualCardsRepository.existsByCardGuid(extract.getCardId()))
				.toList();

			if (!invalidExtracts.isEmpty()) {
				this.logUtility.logMessage("Invalid Auth records (CardId not found in virtual cards): "
						+ invalidExtracts.stream().map(AuthExtract::getCardId).toList(), WARN, log);
				for (AuthExtract e : invalidExtracts) {
					errorMap.put(e.getReceiptId(), "CardId '" + e.getCardId() + "' not found in virtual cards");
				}
				Set<String> invalidReceiptIds = invalidExtracts.stream()
					.map(AuthExtract::getReceiptId)
					.collect(Collectors.toSet());
				authEventExtracts = authEventExtracts.stream()
					.filter((e) -> !invalidReceiptIds.contains(e.getReceiptId()))
					.toList();
				this.logUtility.logMessage("Skipping " + invalidExtracts.size() + " invalid record(s)", INFO, log);
			}

			if (!authEventExtracts.isEmpty()) {
				List<AuthExtract> newExtracts = getNewExtracts(authEventExtracts);
				if (!newExtracts.isEmpty()) {
					processNewExtracts(newExtracts, filePath, errorMap);
				}
				List<AuthExtract> existingExtracts = getUpdatedExistingExtracts(authEventExtracts);
				if (!existingExtracts.isEmpty()) {
					this.authExtractRepository.saveAll(existingExtracts);
					this.logUtility.logMessage(
							"Successfully updated " + existingExtracts.size() + " existing records from the Auth file.",
							INFO, log);
				}
				this.authExtractRepository.saveAll(newExtracts);
				this.logUtility.logMessage("Inserted " + newExtracts.size() + " new records from the Auth file.", INFO,
						log);
				this.fileProcessingStatusLogger.logFileProcessed(filePath, AppConstants.AUTH, totalRecordCount,
						errorMap);

			}
			else {
				String emptyMsg = String.format(NO_VALID_RECORDS_MSG_TEMPLATE, AppConstants.AUTH, filePath);
				this.logUtility.logMessageWithPriority(emptyMsg, WARN, log, P3);
				this.fileProcessingStatusLogger.logFileProcessed(filePath, AppConstants.AUTH, totalRecordCount,
						errorMap);
			}

		}
		catch (IOException ex) {
			String errorMsg = String.format("Failed to process Auth file at path '%s'. Exception: %s", filePath,
					ex.getMessage());
			this.logUtility.logMessageWithPriority(errorMsg, ERROR, log, P3);
			this.fileProcessingStatusLogger.logFileFailed(filePath, AppConstants.AUTH, authEventExtracts.size(),
					ex.getMessage(), null);
			throw new InvalidAuthFileException("Auth extracts file process failed: " + ex.getMessage());
		}
	}

	private List<AuthExtract> getNewExtracts(List<AuthExtract> validExtracts) {
		return validExtracts.stream()
			.filter((extract) -> this.authExtractRepository.findByReceiptId(extract.getReceiptId()) == null)
			.toList();
	}

	private void processNewExtracts(List<AuthExtract> newExtracts, String filePath, Map<String, String> errorMap) {
		newExtracts.forEach((extract) -> {
			try {
				if (StringUtils.isEmpty(extract.getReceiptId())) {
					errorMap.put(extract.getReceiptId(), "ReceiptId is empty or null");
					this.auditLogger.logAuthRecordFailure(extract.getCardId(), extract.getReceiptId(),
							"ReceiptId is empty or null");
				}
				else {
					Optional<VirtualCard> virtualCard = this.virtualCardsRepository.findByCardGuid(extract.getCardId());
					virtualCard.ifPresentOrElse((vc) -> {
						if (APPROVED.equalsIgnoreCase(extract.getAuthActionCode())) {
							this.virtualCardBalanceService.updateBalanceForAuthorization(vc, extract.getAuthAmount(),
									AppConstants.DEBIT, extract.getReceiptId());
							this.auditLogger.logAuthAccepted(extract.getCardId(), vc.getPaymentId(),
									extract.getReceiptId());
							this.sendAuthEventExtractNotification(extract, virtualCard.orElse(new VirtualCard()));
						}
						else {
							this.sendAuthEventExtractNotification(extract, virtualCard.orElse(new VirtualCard()));
							this.auditLogger.logAuthRejected(extract.getCardId(), vc.getPaymentId(),
									extract.getReceiptId());
						}
					}, () -> {
						log.warn(
								"Virtual card not found for cardId {} in Auth extract (filePath: {}, receiptId: {}). Authorization balance update skipped.",
								extract.getCardId(), filePath, extract.getReceiptId());
						errorMap.put(extract.getReceiptId(), "Virtual card not found");
					});

				}
			}
			catch (Exception ex) {
				errorMap.put(extract.getReceiptId(), "Processing error: " + ex.getMessage());
				this.auditLogger.logAuthRecordFailure(extract.getCardId(), extract.getReceiptId(), ex.getMessage());
			}
		});
		this.logUtility.logMessage("Successfully saved " + newExtracts.size() + " new records from the Auth file.",
				INFO, log);
		if (!errorMap.isEmpty()) {
			StringBuilder errorDetails = new StringBuilder("Errors found in Auth file:\n");
			errorMap.forEach((key,
					value) -> errorDetails.append("ReceiptId: ").append(key).append(" - ").append(value).append("\n"));
			this.logUtility.logMessageWithPriority(errorDetails.toString(), ERROR, log, P3);
		}
	}

	private List<AuthExtract> getUpdatedExistingExtracts(List<AuthExtract> validExtracts) {
		return validExtracts.stream().map((extract) -> {
			AuthExtract existing = this.authExtractRepository.findByReceiptId(extract.getReceiptId());
			if (existing != null) {
				// Preserve immutable/status fields from existing record
				extract.setTransactionStatus(existing.getTransactionStatus());
				extract.setTransactionType(existing.getTransactionType());
				extract.setCreatedBy(existing.getCreatedBy());
				extract.setCreatedAt(existing.getCreatedAt());
				extract.setClearingStatus(existing.getClearingStatus()); // Preserve
																			// clearing
																			// status
				existing = this.cardDetailsMapper.modelMapper(extract, AuthExtract.class);
				existing.setUpdatedBy(AUTH_FILE_EXTRACT_PROCESSOR);
				existing.setUpdatedAt(LocalDateTime.now());
			}
			return existing;
		}).filter(Objects::nonNull).toList();
	}

	private AuthExtract mapRecordToEntity(CSVRecord csvRecord, Map<String, String> errorMap) {
		try {
			AuthExtract entity = new AuthExtract();

			entity.setPaymentRequestId(csvRecord.get("payment_request_id"));
			entity.setNetworkTransactionId(csvRecord.get("network_transaction_id"));
			entity.setReceiptId(csvRecord.get(RECEIPT_ID));
			entity.setOriginalAuthRequestId(csvRecord.get("original_auth_id"));
			entity.setRetrievalReferenceNumber(csvRecord.get("retrieval_reference_number"));
			entity.setStan(csvRecord.get("stan"));
			entity.setApprovalCode(csvRecord.get("approval_code"));
			entity.setSystemAuthTime(parseDateTime(csvRecord.get("system_auth_time")));
			entity.setMerchantAuthTime(csvRecord.get("merchant_auth_time"));
			entity.setAuthExpiryTime(csvRecord.get("auth_expiry_time"));
			entity.setBin(csvRecord.get("bin"));
			entity.setIfiId(csvRecord.get("ifi_id"));
			entity.setIfiName(csvRecord.get("ifi_name"));
			entity.setCardId(parseUUID(csvRecord.get("card_id")));
			entity.setCardSequenceNumber(csvRecord.get("card_sequence_number"));
			entity.setPaymentNetwork(csvRecord.get("payment_network"));
			entity.setMaskedCardNumber(csvRecord.get("masked_card_number"));
			entity.setMti(csvRecord.get("mti"));
			BigDecimal authAmount = parseAmountValue(csvRecord.get("auth_amount"));
			BigDecimal convertedAuthAmount = AmountConverter
				.toDollars((authAmount != null) ? authAmount.longValue() : 0L);
			entity.setAuthAmount(convertedAuthAmount);
			entity.setAuthCurrency(csvRecord.get("auth_currency"));
			BigDecimal billingAmount = parseAmountValue(csvRecord.get("billing_amount"));
			BigDecimal convertedBillingAmount = AmountConverter
				.toDollars((billingAmount != null) ? billingAmount.longValue() : 0L);
			entity.setBillingAmount(convertedBillingAmount);
			entity.setBillingCurrency(csvRecord.get("billing_currency"));
			entity.setGeoIndicator(csvRecord.get("geo_indicator"));
			entity.setDccIndicator(csvRecord.get("dcc_indicator"));
			entity.setPosEntryMode(csvRecord.get("pos_entry_mode"));
			entity.setPosTransactionCode(csvRecord.get("pos_transaction_code"));
			entity.setPinPresentIndicator(csvRecord.get("pin_present_indicator"));
			entity.setPinValidIndicator(csvRecord.get("pin_valid_indicator"));
			entity.setCvv2PresentIndicator(csvRecord.get("cvv2_present_indicator"));
			entity.setCvv2ValidIndicator(csvRecord.get("cvv2_valid_indicator"));
			entity.setEcomIndicator(csvRecord.get("ecom_indicator"));
			entity.setAcsCorrelationId(csvRecord.get("acs_correlation_id"));
			entity.setEciIndicator(csvRecord.get("eci_indicator"));
			entity.setAuthActionCode(csvRecord.get("auth_action_code"));
			entity.setResponseCode(csvRecord.get("response_code"));
			entity.setDeclineReason(csvRecord.get("decline_reason"));
			entity.setMotoIndicator(csvRecord.get("moto_indicator"));
			entity.setAcquiringInstitutionId(csvRecord.get("acquiring_institution_id"));
			entity.setForwardingInstitutionId(csvRecord.get("forwarding_institution_id"));
			entity.setMerchantId(csvRecord.get("merchant_id"));
			entity.setMerchantTerminalId(csvRecord.get("merchant_terminal_id"));
			entity.setMerchantCategoryCode(csvRecord.get("merchant_category_code"));
			entity.setMerchantName(csvRecord.get("merchant_name"));
			entity.setMerchantCity(csvRecord.get("merchant_city"));
			entity.setMerchantState(csvRecord.get("merchant_state"));
			entity.setMerchantCountry(csvRecord.get("merchant_country"));
			entity.setPaymentCode(csvRecord.get("payment_code"));
			entity.setPaymentCodeName(csvRecord.get("payment_code_name"));
			entity.setChannelCode(csvRecord.get("channel_code"));
			entity.setChannelCodeName(csvRecord.get("channel_code_name"));
			entity.setCreatedAt(LocalDateTime.now());
			entity.setCreatedBy(AUTH_FILE_EXTRACT_PROCESSOR);

			return entity;
		}
		catch (Exception ex) {
			String errorMsg = String.format(
					"Failed to map AuthExtract CSV record #%d. PaymentRequestId: %s, CardId: %s, ReceiptId: %s. Exception: %s",
					csvRecord.getRecordNumber(), csvRecord.get("payment_request_id"), csvRecord.get("card_id"),
					csvRecord.get(RECEIPT_ID), ex.getMessage());
			this.logUtility.logMessage(errorMsg, ERROR, log);
			errorMap.put(String.valueOf(csvRecord.get(RECEIPT_ID)), errorMsg);
			return null;
		}
	}

	private OffsetDateTime parseDateTime(String value) {
		if (value == null || value.isEmpty()) {
			return null;
		}
		return OffsetDateTime.parse(value);
	}

	private BigDecimal parseAmountValue(String value) {
		return (value == null || value.isEmpty()) ? null : new BigDecimal(value).setScale(2, RoundingMode.DOWN);
	}

	private UUID parseUUID(String value) {
		return (value == null || value.isEmpty()) ? null : UUID.fromString(value);
	}

	private void sendAuthEventExtractNotification(AuthExtract authEventExtract, VirtualCard virtualCard) {
		String type = CARD + DOT_DELIMITER + AUTH + DOT_DELIMITER + ((authEventExtract.getAuthActionCode() == null) ? ""
				: authEventExtract.getAuthActionCode().toLowerCase());
		this.logUtility.logMessage("Sending " + type + " webhook for receiptId: " + authEventExtract.getReceiptId(),
				INFO, log);
		PaymentRequest paymentRequest = this.paymentRequestRepository.findByPaymentId(virtualCard.getPaymentId());
		CardEventWebhookBaseDto cardEventDto = (CardEventWebhookBaseDto) WebhookRequestUtil
			.getCardEventWebhookBaseDto(type, virtualCard.getPaymentId(), null, authEventExtract.getPaymentRequestId());
		MerchantInfo merchantInfo = new MerchantInfo();
		merchantInfo.setMerchantId(authEventExtract.getMerchantId());
		merchantInfo.setMerchantName(authEventExtract.getMerchantName());
		merchantInfo.setMerchantCity(authEventExtract.getMerchantCity());
		merchantInfo.setMerchantState(authEventExtract.getMerchantState());
		merchantInfo.setMerchantCountry(authEventExtract.getMerchantCountry());
		merchantInfo.setMerchantCategoryCode(authEventExtract.getMerchantCategoryCode());

		CardEventWebhookPayload cardEventWebhookPayload = WebhookRequestUtil.getCardEventWebhookPayload(
				authEventExtract.getBin(), authEventExtract.getMaskedCardNumber(), cardEventDto.getType(),
				authEventExtract.getReceiptId(), authEventExtract.getAuthAmount(), virtualCard, merchantInfo,
				authEventExtract.getApprovalCode(), authEventExtract.getAuthExpiryTime(),
				authEventExtract.getResponseCode(), authEventExtract.getDeclineReason());
		cardEventDto.setCard(cardEventWebhookPayload);

		try {
			String authExtractJson = this.mapper.writerWithDefaultPrettyPrinter().writeValueAsString(cardEventDto);
			this.logUtility.logMessage("Sending " + type + " webhook for receiptId: " + authExtractJson, INFO, log);
		}
		catch (JsonProcessingException ex) {
			this.logUtility.logMessage(ERROR_CONVERTING_DTO_TO_JSON + ex.getMessage(), ERROR, log);
		}

		this.webhookClient.notifyClient(cardEventDto, paymentRequest.getOrgShortCode(),
				this.cardAuthApprovedNotificationEndpoint);
	}

}
