package com.uhg.optum.financials.payments.payer.utils;

import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.io.IOException;
import java.math.BigDecimal;
import java.math.RoundingMode;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.LinkedHashMap;
import java.util.Locale;
import java.util.Map;
import java.util.UUID;

@Service
public class ClearExtractFieldMapper {

	private static final String DEBIT = "DEBIT";

	private static final String CREDIT = "CREDIT";

	@Autowired
	private CsvFileWriter csvFileWriter;

	@Autowired
	private AuthExtractRepository authExtractRepository;

	public String clearFieldMapper(String receiptId, String settlementIndicator, String cardId) throws IOException {
		UUID cardUuid = null;
		try {
			cardUuid = UUID.fromString(cardId);
		}
		catch (IllegalArgumentException exception) {
			throw new IllegalArgumentException("Invalid cardId format. Must be a valid UUID string: " + cardId);
		}

		AuthExtract authExtract = this.authExtractRepository.findByReceiptIdAndCardId(receiptId, cardUuid);
		if (authExtract == null) {
			throw new IllegalArgumentException("Auth extract not found for receiptId: " + receiptId);
		}

		String normalizedSettlementIndicator = normalizeSettlementIndicator(settlementIndicator);
		LocalDateTime now = LocalDateTime.now();
		String fileTs = now.format(DateTimeFormatter.ofPattern("dd-MMM-yyyy_HH_mm_ss", Locale.ENGLISH));
		String batchTs = now.format(DateTimeFormatter.ofPattern("yyMMddHHmmss", Locale.ENGLISH));
		String settlementDate = now.format(DateTimeFormatter.ofPattern("yyMMdd", Locale.ENGLISH));
		String purchaseDate = now.format(DateTimeFormatter.ofPattern("MMdd", Locale.ENGLISH));
		String clearingDate = now.format(DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss", Locale.ENGLISH));
		BigDecimal clearingAmount = (authExtract.getAuthAmount() != null) ? authExtract.getAuthAmount()
				: BigDecimal.TEN;
		String clearingAmountInMinorUnits = toMinorUnits(clearingAmount);
		String[] clearingInfo = deriveClearingInfo(normalizedSettlementIndicator);

		var csv = this.csvFileWriter.buildWorkingFilePath("Clearance_Extract_" + fileTs + ".csv");
		Map<String, String> row = new LinkedHashMap<>();
		row.put("file_id", "001" + settlementDate + "000SIM" + RandomDataGenerator.randomDigits(10));
		row.put("file_name",
				"MASTERCARD_CLEARANCE_REPORT_SIM_" + batchTs + "/MCI.AR.T112.SIM.301117." + batchTs + ".A053");
		row.put("message_sequence_number", RandomDataGenerator.randomDigits(8));
		row.put("network", defaultValue(authExtract.getPaymentNetwork(), "MASTERCARD"));
		row.put("network_transaction_id",
				defaultValue(authExtract.getNetworkTransactionId(), RandomDataGenerator.uuidPrefix("NET")));
		row.put("receipt_id", RandomDataGenerator.randomDigits(10));
		row.put("original_auth_id",
				defaultValue(authExtract.getOriginalAuthRequestId(), RandomDataGenerator.randomAlphaNumeric(12)));
		row.put("acquirer_institution_id", defaultValue(authExtract.getAcquiringInstitutionId(), "968500"));
		row.put("ifi_id", defaultValue(authExtract.getIfiId(), "600710"));
		row.put("ifi_name", defaultValue(authExtract.getIfiName(), "optumbankdemo"));
		row.put("bin", defaultValue(authExtract.getBin(), "111111"));
		row.put("card_id", String.valueOf(authExtract.getCardId()));
		row.put("masked_pan_number", defaultValue(authExtract.getMaskedCardNumber(), "111111-xxxxxx-0000"));
		row.put("card_status", "ENABLED");
		row.put("account_holder_id", RandomDataGenerator.uuid());
		row.put("settlement_date", settlementDate);
		row.put("settlement_indicator", normalizedSettlementIndicator);
		row.put("approval_code", defaultValue(authExtract.getApprovalCode(),
				CREDIT.equals(normalizedSettlementIndicator) ? "0" : RandomDataGenerator.randomDigits(6)));
		row.put("retrieval_reference_number",
				defaultValue(authExtract.getRetrievalReferenceNumber(), RandomDataGenerator.randomDigits(12)));
		row.put("acquirer_reference_number", RandomDataGenerator.randomDigits(23));
		row.put("clearing_status", "SUCCESS");
		row.put("clearing_type", clearingInfo[0]);
		row.put("clearing_sub_category", clearingInfo[1]);
		row.put("purchase_date", purchaseDate);
		row.put("clearing_date", clearingDate);
		row.put("clearing_amount", clearingAmountInMinorUnits);
		row.put("clearing_currency", "840");
		row.put("billing_amount", clearingAmountInMinorUnits);
		row.put("billing_currency", "840");
		row.put("failure_reason", "");
		row.put("geo_indicator", defaultValue(authExtract.getGeoIndicator(), "INTERNATIONAL"));
		row.put("merchant_id", defaultValue(authExtract.getMerchantId(), ""));
		row.put("merchant_name", defaultValue(authExtract.getMerchantName(), ""));
		row.put("merchant_terminal_id", defaultValue(authExtract.getMerchantTerminalId(), "TID001"));
		row.put("merchant_category_code", defaultValue(authExtract.getMerchantCategoryCode(), "5611"));
		row.put("merchant_city", defaultValue(authExtract.getMerchantCity(), ""));
		row.put("merchant_country_code", defaultValue(authExtract.getMerchantCountry(), ""));
		row.put("interchange_fee_amount", "");
		row.put("interchange_fee_sign", "");
		row.put("interchange_fee_currency", "");
		row.put("payment_code", defaultValue(authExtract.getPaymentCode(), "PMCUSZZ112910272"));
		row.put("payment_code_name", defaultValue(authExtract.getPaymentCodeName(), "INT_ECOM_CIT_RECURRING_PREAUTH"));
		row.put("channel_code", defaultValue(authExtract.getChannelCode(), "CHNUSZZ1129"));
		row.put("channel_code_name", defaultValue(authExtract.getChannelCodeName(), "MC B2B US Offus Channel"));
		row.put("mti", defaultValue(authExtract.getMti(), "1240"));
		row.put("processing_code", "200000");
		row.put("function_code", "200");

		this.csvFileWriter.writeToCsvFile(csv, row);
		return authExtract.getReceiptId();
	}

	public String[] getClearingInfo(String settlementIndicator) {
		String normalizedSettlementIndicator = normalizeSettlementIndicator(settlementIndicator);
		return deriveClearingInfo(normalizedSettlementIndicator);
	}

	private String normalizeSettlementIndicator(String settlementIndicator) {
		String normalized = (settlementIndicator != null) ? settlementIndicator.trim().toUpperCase(Locale.ENGLISH) : "";
		if (!DEBIT.equals(normalized) && !CREDIT.equals(normalized)) {
			throw new IllegalArgumentException("settlement_indicator must be CREDIT or DEBIT");
		}
		return normalized;
	}

	private String[] deriveClearingInfo(String settlementIndicator) {
		if (CREDIT.equals(settlementIndicator)) {
			return new String[] { "MERCHANDISE_RETURN", "MATCHED_REFUNDS" };
		}
		return new String[] { "FIRST_PRESENTMENT", "MATCHED_AUTH" };
	}

	private String defaultValue(String value, String fallback) {
		return (value == null || value.isBlank()) ? fallback : value;
	}

	private String toMinorUnits(BigDecimal amount) {
		if (amount == null) {
			return "";
		}
		return amount.movePointRight(2).setScale(0, RoundingMode.DOWN).toPlainString();
	}

}
