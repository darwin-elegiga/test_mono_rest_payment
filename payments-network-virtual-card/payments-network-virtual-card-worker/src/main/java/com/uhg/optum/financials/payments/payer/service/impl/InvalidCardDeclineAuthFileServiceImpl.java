package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobContainerClient;
import com.uhg.optum.financials.payments.payer.exception.InvalidCardDeclineAuthFileException;
import com.uhg.optum.financials.payments.payer.model.InvalidCardDeclineEvents;
import com.uhg.optum.financials.payments.payer.repository.InvalidCardDeclineEventsRepository;
import com.uhg.optum.financials.payments.payer.service.InvalidCardDeclineAuthFileService;
import com.uhg.optum.financials.payments.payer.util.FileProcessingStatusLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.core.storage.AzureStorageUtil;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.csv.CSVFormat;
import org.apache.commons.csv.CSVParser;
import org.apache.commons.csv.CSVRecord;
import org.apache.commons.io.input.BOMInputStream;
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

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INVALID_CARD_AUTH_DECLINE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INVALID_CARD_DECLINE_AUTH_EXTRACT_PROCESSOR;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;

@Service
@Slf4j
public class InvalidCardDeclineAuthFileServiceImpl implements InvalidCardDeclineAuthFileService {

	private final BlobContainerClient blobContainerClient;

	private final LogUtility logUtility;

	private final InvalidCardDeclineEventsRepository invalidCardDeclineEventsRepository;

	private final FileProcessingStatusLogger fileProcessingStatusLogger;

	public InvalidCardDeclineAuthFileServiceImpl(BlobContainerClient blobContainerClient, LogUtility logUtility,
			InvalidCardDeclineEventsRepository invalidCardDeclineEventsRepository,
			FileProcessingStatusLogger fileProcessingStatusLogger) {
		this.blobContainerClient = blobContainerClient;
		this.logUtility = logUtility;
		this.invalidCardDeclineEventsRepository = invalidCardDeclineEventsRepository;
		this.fileProcessingStatusLogger = fileProcessingStatusLogger;
	}

	@Override
	public void processInvalidAuthDeclineFile(String filePath) {
		this.logUtility.logMessage("Processing Auth file at: " + filePath, INFO, log);
		List<InvalidCardDeclineEvents> invalidCardDeclineEvents = new ArrayList<>();
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
			this.logUtility.logMessage("Mapping csv file to InvalidCardDeclineAuthFile model: ", INFO, log);
			invalidCardDeclineEvents = csvRecords.stream()
				.filter((csvRecord) -> csvRecord.stream().anyMatch((value) -> value != null && !value.isEmpty()))
				.map((rec) -> mapRecordToEntity(rec, errorMap))
				.filter(Objects::nonNull)
				.toList();

			this.invalidCardDeclineEventsRepository.saveAll(invalidCardDeclineEvents);
			this.logUtility.logMessage("Invalid Card Decline Auth Event info saved to table ", INFO, log);
			this.fileProcessingStatusLogger.logFileProcessed(filePath, INVALID_CARD_AUTH_DECLINE, totalRecordCount,
					errorMap);
			if (!errorMap.isEmpty()) {
				StringBuilder errorDetails = new StringBuilder("Errors found in Invalid Card Decline Auth file:\n");
				errorMap.forEach((key, value) -> errorDetails.append("NetworkTransactionId: ")
					.append(key)
					.append(" - ")
					.append(value)
					.append("\n"));
				this.logUtility.logMessageWithPriority(errorDetails.toString(), ERROR, log, P3);
			}
		}
		catch (IOException ex) {
			String errorMsg = String.format(
					"Failed to process Invalid Card Decline Auth file at path '%s'. Exception: %s", filePath,
					ex.getMessage());
			this.logUtility.logMessageWithPriority(errorMsg, ERROR, log, P3);
			this.fileProcessingStatusLogger.logFileFailed(filePath, INVALID_CARD_AUTH_DECLINE, totalRecordCount,
					ex.getMessage(), errorMap);
			throw new InvalidCardDeclineAuthFileException(
					"Invalid Card Decline Auth File process failed: " + ex.getMessage());
		}
	}

	private InvalidCardDeclineEvents mapRecordToEntity(CSVRecord csvRecord, Map<String, String> errorMap) {
		try {
			InvalidCardDeclineEvents entity = new InvalidCardDeclineEvents();

			entity.setNetworkTransactionId(csvRecord.get("network_transaction_id"));
			entity.setRetrievalReferenceNumber(csvRecord.get("retrieval_reference_number"));
			entity.setStan(csvRecord.get("stan"));
			String systemAuthTime = csvRecord.get("system_auth_time");
			LocalDateTime localDateTime = parseDateTime(systemAuthTime);
			entity.setSystemAuthTime(localDateTime);
			entity.setTimeLocalTransaction(getIntegerValue(csvRecord.get("time_local_transaction")));
			entity.setDateLocalTransaction(getIntegerValue(csvRecord.get("date_local_transaction")));
			entity.setBin(csvRecord.get("bin"));
			entity.setIfiId(csvRecord.get("ifi_id"));
			entity.setIfiName(csvRecord.get("ifi_name"));
			entity.setCardSequenceNumber(csvRecord.get("card_sequence_number"));
			entity.setPaymentNetwork(csvRecord.get("payment_network"));
			entity.setMaskedCardNumber(csvRecord.get("masked_card_number"));
			entity.setMti(csvRecord.get("mti"));
			entity.setAuthAmount(parseAmountValue(csvRecord.get("auth_amount")));
			entity.setAuthCurrency(csvRecord.get("auth_currency"));
			entity.setBillingAmount(parseAmountValue(csvRecord.get("billing_amount")));
			entity.setBillingCurrency(csvRecord.get("billing_currency"));
			entity.setAdditionalDataPrivateUse(csvRecord.get("additional_data_private_use"));
			entity.setPosEntryMode(csvRecord.get("pos_entry_mode"));
			entity.setPosData(csvRecord.get("pos_data"));
			entity.setAuthActionCode(csvRecord.get("auth_action_code"));
			entity.setResponseCode(csvRecord.get("response_code"));
			entity.setAcquiringInstitutionId(csvRecord.get("acquiring_institution_id"));
			entity.setForwardingInstitutionId(csvRecord.get("forwarding_institution_id"));
			entity.setMerchantId(csvRecord.get("merchant_id"));
			entity.setMerchantTerminalId(csvRecord.get("merchant_terminal_id"));
			entity.setMerchantCategoryCode(csvRecord.get("merchant_category_code"));
			entity.setMerchantNameLocation(csvRecord.get("merchant_name_location"));
			entity.setCreatedAt(LocalDateTime.now());
			entity.setCreatedBy(INVALID_CARD_DECLINE_AUTH_EXTRACT_PROCESSOR);
			return entity;
		}
		catch (Exception ex) {
			String errorMsg = String.format(
					"Failed to map CSV record #%d for Network Transaction ID '%s'. Exception: %s",
					csvRecord.getRecordNumber(), csvRecord.get("network_transaction_id"), ex.getMessage());
			this.logUtility.logMessage(errorMsg, ERROR, log);
			errorMap.put(String.valueOf(csvRecord.get("network_transaction_id")), errorMsg);
			return null;
		}
	}

	private LocalDateTime parseDateTime(String value) {
		if (value == null || value.isEmpty()) {
			return null;
		}

		OffsetDateTime offsetDateTime = OffsetDateTime.parse(value);
		return offsetDateTime.toLocalDateTime();
	}

	private BigDecimal parseAmountValue(String value) {
		return (value == null || value.isEmpty()) ? null : new BigDecimal(value).setScale(2, RoundingMode.DOWN);
	}

	private Integer getIntegerValue(String value) {
		return (value == null || value.isEmpty()) ? null : Integer.parseInt(value);
	}

}
