package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobContainerClient;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardWebhookDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.LoadingWebhookRequestDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.LoadingWebhookRequestPayload;
import com.uhg.optum.financials.payments.payer.dto.webhook.PaymentNetworkMetadataDto;
import com.uhg.optum.financials.payments.payer.exception.InvalidClearFileException;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.ClearExtract;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.ClearExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.ClearFileService;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.util.AmountConverter;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.FileProcessingStatusLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import com.uhg.optum.financials.payments.payer.util.WebhookRequestUtil;
import com.uhg.optum.financials.shared.core.storage.AzureStorageUtil;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.csv.CSVFormat;
import org.apache.commons.csv.CSVParser;
import org.apache.commons.csv.CSVRecord;
import org.apache.commons.io.input.BOMInputStream;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.math.BigDecimal;
import java.nio.charset.StandardCharsets;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeFormatterBuilder;
import java.time.temporal.ChronoField;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import java.util.stream.Collectors;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ACTIVE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.AUTH_CLEAR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CLEAR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CLEAR_FILE_EXTRACT_PROCESSOR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CREDIT;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.DEBIT;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_CONVERTING_DTO_TO_JSON;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.NO_VALID_RECORDS_MSG_TEMPLATE;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.WARN;
import static com.uhg.optum.financials.payments.payer.constant.Constants.DOT_DELIMITER;
import static com.uhg.optum.financials.payments.payer.constant.Constants.LOADING;
import static com.uhg.optum.financials.payments.payer.constant.Constants.PAYMENT;
import static com.uhg.optum.financials.payments.payer.constant.Constants.REFUND;
import static com.uhg.optum.financials.payments.payer.constant.Constants.SETTLED;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.enums.Status.PS;

@Service
@Slf4j
public class ClearFileServiceImpl implements ClearFileService {

	private final ClearExtractRepository clearExtractRepository;

	private final AuthExtractRepository authExtractRepository;

	private final BlobContainerClient blobContainerClient;

	private final LogUtility logUtility;

	private final WebhookClient webhookClient;

	private final PaymentRequestRepository paymentRequestRepository;

	private final VirtualCardsRepository virtualCardsRepository;

	private final VirtualCardBalanceService virtualCardBalanceService;

	private final FileProcessingStatusLogger fileProcessingStatusLogger;

	@Value("${endpoint.issuing.loadingNotification}")
	private String cardSettledNotificationEndpoint;

	private final AuditLogger auditLogger;

	private final VirtualCardUtil virtualCardUtil;

	public ClearFileServiceImpl(ClearExtractRepository clearExtractRepository,
			AuthExtractRepository authExtractRepository, BlobContainerClient blobContainerClient, LogUtility logUtility,
			WebhookClient webhookClient, PaymentRequestRepository paymentRequestRepository,
			VirtualCardsRepository virtualCardsRepository, VirtualCardBalanceService virtualCardBalanceService,
			AuditLogger auditLogger, FileProcessingStatusLogger fileProcessingStatusLogger,
			VirtualCardUtil virtualCardUtil) {
		this.clearExtractRepository = clearExtractRepository;
		this.authExtractRepository = authExtractRepository;
		this.blobContainerClient = blobContainerClient;
		this.logUtility = logUtility;
		this.webhookClient = webhookClient;
		this.paymentRequestRepository = paymentRequestRepository;
		this.virtualCardsRepository = virtualCardsRepository;
		this.virtualCardBalanceService = virtualCardBalanceService;
		this.auditLogger = auditLogger;
		this.fileProcessingStatusLogger = fileProcessingStatusLogger;
		this.virtualCardUtil = virtualCardUtil;
	}

	@Override
	public void processClearFile(String filePath) {
		this.logUtility.logMessage("Processing Clear file at: " + filePath, "info", log);
		List<ClearExtract> clearExtracts = new ArrayList<>();
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

			clearExtracts = csvRecords.stream()
				.filter((csvRecord) -> csvRecord.stream().anyMatch((value) -> value != null && !value.isEmpty()))
				.map((record) -> mapRecordToEntity(record, errorMap))
				.filter(Objects::nonNull)
				.toList();

			if (!clearExtracts.isEmpty()) {
				clearExtracts = removeDuplicates(clearExtracts, errorMap);

				List<ClearExtract> processedExtracts = clearExtracts.stream()
					.map((extract) -> processExtract(extract, errorMap))
					.filter(Objects::nonNull)
					.toList();

				this.clearExtractRepository.saveAll(processedExtracts);
				this.logUtility.logMessage(
						"Successfully saved " + processedExtracts.size() + " new records from the Clear file.", INFO,
						log);
				this.fileProcessingStatusLogger.logFileProcessed(filePath, CLEAR, totalRecordCount, errorMap);
				if (!errorMap.isEmpty()) {
					StringBuilder errorDetails = new StringBuilder("Errors found in Clear file:\n");
					errorMap.forEach((key, value) -> errorDetails.append("ReceiptId: ")
						.append(key)
						.append(" - ")
						.append(value)
						.append("\n"));
					this.logUtility.logMessageWithPriority(errorDetails.toString(), ERROR, log, P3);
				}
			}
			else {
				String emptyMsg = String.format(NO_VALID_RECORDS_MSG_TEMPLATE, CLEAR, filePath);
				this.logUtility.logMessageWithPriority(emptyMsg, WARN, log, P3);
				this.fileProcessingStatusLogger.logFileProcessed(filePath, CLEAR, totalRecordCount, errorMap);
			}
		}
		catch (IOException ex) {
			String errorMsg = String.format("Failed to process Clear file at path '%s'. Exception: %s", filePath,
					ex.getMessage());
			this.logUtility.logMessageWithPriority(errorMsg, ERROR, log, P3);
			this.fileProcessingStatusLogger.logFileFailed(filePath, CLEAR, totalRecordCount, ex.getMessage(), errorMap);
			throw new InvalidClearFileException("Clear extracts file process failed: " + ex.getMessage());
		}
	}

	private void handleClearingAmounts(ClearExtract extract, VirtualCard virtualCard, AuthExtract authExtract,
			String receiptId, boolean isClearOnlyRecord, BigDecimal forceSettlementAmount) {

		VirtualCard updatedVirtualCard = this.virtualCardBalanceService.updateBalanceForClearing(virtualCard,
				Optional.ofNullable(extract.getClearingAmount()).orElse(BigDecimal.ZERO),
				extract.getSettlementIndicator(), isClearOnlyRecord, receiptId, forceSettlementAmount);
		if (updatedVirtualCard != null) {
			virtualCard = updatedVirtualCard;
		}
		this.auditLogger.logClearRecordSuccess(extract.getCardId(), virtualCard.getPaymentId(), extract.getReceiptId());
		if (authExtract != null) {
			this.authExtractRepository.save(authExtract);
		}

		// If the card balance is zero after clearing, update payment request status to
		// 'PS' (Settled)
		if (virtualCard.getCurrentBalance().compareTo(BigDecimal.ZERO) == 0) {
			this.virtualCardUtil.updatePaymentRequestStatus(virtualCard.getPaymentId(), PS);
			this.logUtility.logMessage("Payment settled for Virtual Card with cardId: " + virtualCard.getCardGuid(),
					INFO, log);
		}

		this.sendClearEventExtractNotification(extract, virtualCard, receiptId);
	}

	private ClearExtract mapRecordToEntity(CSVRecord csvRecord, Map<String, String> errorMap) {
		try {
			ClearExtract entity = new ClearExtract();
			entity.setFileId(csvRecord.get("file_id"));
			entity.setFileName(csvRecord.get("file_name"));
			entity.setMessageSequenceNumber(csvRecord.get("message_sequence_number"));
			entity.setOriginalAuthId(csvRecord.get("original_auth_id"));
			entity.setNetwork(csvRecord.get("network"));
			entity.setNetworkTransactionId(csvRecord.get("network_transaction_id"));
			entity.setReceiptId(csvRecord.get("receipt_id"));
			entity.setAcquirerInstitutionId(csvRecord.get("acquirer_institution_id"));
			entity.setIfiId(csvRecord.get("ifi_id"));
			entity.setIfiName(csvRecord.get("ifi_name"));
			entity.setBin(csvRecord.get("bin"));
			entity.setCardId(parseUUID(csvRecord.get("card_id")));
			entity.setMaskedPanNumber(csvRecord.get("masked_pan_number"));
			entity.setCardStatus(csvRecord.get("card_status"));
			entity.setAccountHolderId(csvRecord.get("account_holder_id"));
			entity.setSettlementDate(csvRecord.get("settlement_date"));
			entity.setSettlementIndicator(csvRecord.get("settlement_indicator"));
			entity.setApprovalCode(csvRecord.get("approval_code"));
			entity.setRetrievalReferenceNumber(csvRecord.get("retrieval_reference_number"));
			entity.setAcquirerReferenceNumber(csvRecord.get("acquirer_reference_number"));
			entity.setClearingStatus(csvRecord.get("clearing_status"));
			entity.setClearingType(csvRecord.get("clearing_type"));
			entity.setClearingSubCategory(csvRecord.get("clearing_sub_category"));
			entity.setPurchaseDate(csvRecord.get("purchase_date"));
			entity.setClearingDate(parseDateTime(csvRecord.get("clearing_date")));
			BigDecimal clearingAmount = parseAmountValue(csvRecord.get("clearing_amount"));
			BigDecimal convertedClearingAmount = AmountConverter
				.toDollars((clearingAmount != null) ? clearingAmount.longValue() : 0L);
			entity.setClearingAmount(convertedClearingAmount);
			entity.setClearingCurrency(csvRecord.get("clearing_currency"));
			BigDecimal billingAmount = parseAmountValue(csvRecord.get("billing_amount"));
			BigDecimal convertedBillingAmount = AmountConverter
				.toDollars((billingAmount != null) ? billingAmount.longValue() : 0L);
			entity.setBillingAmount(convertedBillingAmount);
			entity.setBillingCurrency(csvRecord.get("billing_currency"));
			entity.setFailureReason(csvRecord.get("failure_reason"));
			entity.setGeoIndicator(csvRecord.get("geo_indicator"));
			entity.setMerchantId(csvRecord.get("merchant_id"));
			entity.setMerchantName(csvRecord.get("merchant_name"));
			entity.setMerchantTerminalId(csvRecord.get("merchant_terminal_id"));
			entity.setMerchantCategoryCode(csvRecord.get("merchant_category_code"));
			entity.setMerchantCity(csvRecord.get("merchant_city"));
			entity.setMerchantCountryCode(csvRecord.get("merchant_country_code"));
			entity.setInterchangeFeeAmount(parseAmountValue(csvRecord.get("interchange_fee_amount")));
			entity.setInterchangeFeeSign(csvRecord.get("interchange_fee_sign"));
			entity.setInterchangeFeeCurrency(csvRecord.get("interchange_fee_currency"));
			entity.setPaymentCode(csvRecord.get("payment_code"));
			entity.setPaymentCodeName(csvRecord.get("payment_code_name"));
			entity.setChannelCode(csvRecord.get("channel_code"));
			entity.setChannelCodeName(csvRecord.get("channel_code_name"));
			entity.setMti(csvRecord.get("mti"));
			entity.setProcessingCode(csvRecord.get("processing_code"));
			entity.setFunctionCode(csvRecord.get("function_code"));
			entity.setCreatedAt(LocalDateTime.now());
			entity.setCreatedBy(CLEAR_FILE_EXTRACT_PROCESSOR);
			return entity;
		}
		catch (Exception ex) {
			String errorMsg = String.format("Failed to map CSV record #%d from file '%s'. Exception: %s",
					csvRecord.getRecordNumber(), csvRecord.get("file_name"), ex.getMessage());
			this.logUtility.logMessage(errorMsg, ERROR, log);
			errorMap.put(String.valueOf(csvRecord.get("receipt_id")), errorMsg);
			return null;
		}
	}

	private LocalDateTime parseDateTime(String value) {
		if (value == null || value.isEmpty()) {
			return null;
		}

		DateTimeFormatter formatter = new DateTimeFormatterBuilder().appendPattern("yyyy-MM-dd HH:mm:ss")
			.optionalStart()
			.appendFraction(ChronoField.NANO_OF_SECOND, 1, 9, true)
			.optionalEnd()
			.toFormatter();

		return LocalDateTime.parse(value, formatter);
	}

	private UUID parseUUID(String value) {
		return (value == null || value.isEmpty()) ? null : UUID.fromString(value);
	}

	private BigDecimal parseAmountValue(String value) {
		return ((value == null || value.isEmpty()) ? null
				: new BigDecimal(value).setScale(2, java.math.RoundingMode.DOWN));
	}

	private void sendClearEventExtractNotification(ClearExtract clearEventExtract, VirtualCard virtualCard,
			String receiptId) {
		this.logUtility.logMessage(
				"Sending loading.payment.settled webhook for receiptId: " + clearEventExtract.getReceiptId(), INFO,
				log);

		PaymentRequest paymentRequest = this.paymentRequestRepository.findByPaymentId(virtualCard.getPaymentId());

		LoadingWebhookRequestDto sentSettledDto = (LoadingWebhookRequestDto) WebhookRequestUtil
			.getLoadingWebhookBaseDto(LOADING + DOT_DELIMITER + PAYMENT + DOT_DELIMITER + SETTLED,
					virtualCard.getPaymentId(), paymentRequest.getMetadata(), null, null,
					paymentRequest.getPaymentDetails().getNetworkPaymentId());
		PaymentNetworkMetadataDto paymentNetworkMetadata = WebhookRequestUtil.getPaymentNetworkMetaData(
				clearEventExtract.getCardId(), virtualCard.getInitialBalance(), virtualCard.getCurrentBalance(),
				virtualCard.getAvailableBalance(), clearEventExtract.getBin(),
				clearEventExtract.getMaskedPanNumber().substring(clearEventExtract.getMaskedPanNumber().length() - 4),
				receiptId, ACTIVE);

		LoadingWebhookRequestPayload loadingSentWebhookRequestPayload = WebhookRequestUtil
			.getLoadingWebhookRequestPayload(clearEventExtract.getClearingAmount().doubleValue(), null,
					"Loading " + SETTLED + " Successfully", virtualCard.getAvailableBalance().doubleValue(),
					virtualCard.getExpiryDate());
		if (CREDIT.equalsIgnoreCase(clearEventExtract.getSettlementIndicator())) {
			paymentNetworkMetadata.getCard().setAccountingTransaction(CREDIT);
			loadingSentWebhookRequestPayload.getPaymentActivity().setAction(REFUND);
		}
		else if (DEBIT.equalsIgnoreCase(clearEventExtract.getSettlementIndicator())) {
			paymentNetworkMetadata.getCard().setAccountingTransaction(DEBIT);
			loadingSentWebhookRequestPayload.getPaymentActivity().setAction(SETTLED);
		}
		CardWebhookDto cardWebhookDto = paymentNetworkMetadata.getCard();
		cardWebhookDto.setApprovalCode(clearEventExtract.getApprovalCode());
		cardWebhookDto.setMerchantName(clearEventExtract.getMerchantName());
		cardWebhookDto.setMerchantTerminalId(clearEventExtract.getMerchantTerminalId());
		paymentNetworkMetadata.setCard(cardWebhookDto);
		sentSettledDto.setData(loadingSentWebhookRequestPayload);
		sentSettledDto.setPaymentNetworkMetadata(paymentNetworkMetadata);
		ObjectMapper mapper = new ObjectMapper();

		try {
			String sentSettledDtoJson = mapper.writerWithDefaultPrettyPrinter().writeValueAsString(sentSettledDto);
			this.logUtility.logMessage("Sending loading.settled webhook payload: " + sentSettledDtoJson, INFO, log);
		}
		catch (JsonProcessingException ex) {
			this.logUtility.logMessage(ERROR_CONVERTING_DTO_TO_JSON + ex.getMessage(), ERROR, log);
		}
		this.webhookClient.notifyClient(sentSettledDto, paymentRequest.getOrgShortCode(),
				this.cardSettledNotificationEndpoint);

	}

	private List<ClearExtract> removeDuplicates(List<ClearExtract> clearExtracts, Map<String, String> errorMap) {
		List<ClearExtract> duplicates = clearExtracts.stream()
			.filter((extract) -> this.clearExtractRepository.findByReceiptId(extract.getReceiptId()) != null)
			.toList();

		if (!duplicates.isEmpty()) {
			String errorMessage = "Duplicate records found with ReceiptId(s): "
					+ duplicates.stream().map(ClearExtract::getReceiptId).toList();
			this.logUtility.logMessage(errorMessage, INFO, log);
			duplicates
				.forEach((duplicate) -> errorMap.put(duplicate.getReceiptId(), "Duplicate record (already imported)"));
			Set<String> duplicateIds = duplicates.stream().map(ClearExtract::getReceiptId).collect(Collectors.toSet());
			clearExtracts = clearExtracts.stream()
				.filter((extract) -> !duplicateIds.contains(extract.getReceiptId()))
				.toList();
			this.logUtility.logMessage("Skipping " + duplicates.size() + " duplicate record(s); continuing with "
					+ clearExtracts.size() + " non-duplicate record(s).", INFO, log);
		}
		return clearExtracts;
	}

	private ClearExtract processExtract(ClearExtract extract, Map<String, String> errorMap) {
		try {
			if (extract.getOriginalAuthId() != null) {
				Optional<AuthExtract> authExtracts = this.authExtractRepository
					.findByOriginalAuthRequestId(extract.getOriginalAuthId());
				Optional<VirtualCard> virtualCard = this.virtualCardsRepository.findByCardGuid(extract.getCardId());
				if (virtualCard.isPresent()) {
					ClearExtract processedClearExtract = getProcessedClearExtract(extract, errorMap, authExtracts,
							virtualCard);
					if (processedClearExtract != null) {
						return processedClearExtract;
					}
				}
				else {
					log.warn(
							"Virtual card not found for cardId {} in Clear extract (fileId: {}, receiptId: {}). Clearing balance update skipped.",
							extract.getCardId(), extract.getFileId(), extract.getReceiptId());
					errorMap.put(extract.getReceiptId(), "Virtual card not found");
				}
			}
			else {
				String businessErrorMsg = String.format(
						"Business validation failed: Clear extract with ReceiptId '%s' does not have a corresponding Auth extract for originalAuthId '%s'.",
						extract.getReceiptId(), extract.getOriginalAuthId());
				this.logUtility.logMessageWithPriority(businessErrorMsg, WARN, log, P3);
				errorMap.put(extract.getReceiptId(), "Missing corresponding Auth extract");
			}
		}
		catch (Exception ex) {
			errorMap.put(extract.getReceiptId(), "Processing error: " + ex.getMessage());
			this.auditLogger.logClearRecordFailure(extract.getCardId(), extract.getReceiptId(), ex.getMessage());
		}
		return null;
	}

	private ClearExtract getProcessedClearExtract(ClearExtract extract, Map<String, String> errorMap,
			Optional<AuthExtract> authExtracts, Optional<VirtualCard> virtualCard) {
		if (authExtracts.isEmpty()) {
			extract.setRecordStatus(CLEAR);
			handleClearingAmounts(extract, virtualCard.get(), null, extract.getReceiptId(), true, null);
			return extract;
		}
		else {
			AuthExtract authExtract = authExtracts.get();
			if (extract.getRetrievalReferenceNumber().equalsIgnoreCase(authExtract.getRetrievalReferenceNumber())
					&& !("AUTH_RESET_CLEAR".equalsIgnoreCase(authExtract.getClearingStatus()))) {
				BigDecimal forceSettlementAmount = null;
				if (extract.getClearingAmount().compareTo(authExtract.getAuthAmount()) > 0) {
					forceSettlementAmount = virtualCard.get()
						.getAvailableBalance()
						.subtract(extract.getClearingAmount().subtract(authExtract.getAuthAmount()));
				}
				extract.setRecordStatus(CLEAR);
				authExtract.setClearingStatus(AUTH_CLEAR);
				authExtract.setUpdatedBy(CLEAR_FILE_EXTRACT_PROCESSOR);
				authExtract.setUpdatedAt(LocalDateTime.now());
				handleClearingAmounts(extract, virtualCard.get(), authExtract, authExtract.getReceiptId(), false,
						forceSettlementAmount);
				return extract;
			}
			else {
				String businessErrorMsg = String.format(
						"Business validation failed: Clear extract with ReceiptId '%s' has a mismatched RetrievalReferenceNumber compared to its corresponding Auth extract for originalAuthId '%s' or already cleared.",
						extract.getReceiptId(), extract.getOriginalAuthId());
				this.logUtility.logMessageWithPriority(businessErrorMsg, WARN, log, P3);
				errorMap.put(extract.getReceiptId(), "Mismatched RetrievalReferenceNumber or already cleared");
			}
		}
		return null;
	}

}
