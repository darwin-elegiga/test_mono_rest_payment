
package com.uhg.optum.financials.payments.payer.utils;

import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import org.apache.commons.lang3.StringUtils;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.io.IOException;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.LinkedHashMap;
import java.util.Locale;
import java.util.Map;
import java.util.UUID;

@Service
public class AuthExtractFieldMapper {

	@Autowired
	private CsvFileWriter csvFileWriter;

	@Autowired
	private AuthExtractRepository authExtractRepository;

	public String authFieldMapper(String cardId, String receiptId) throws IOException {
		DateTimeFormatter fmt = DateTimeFormatter.ofPattern("dd-MMM-yyyy_HH_mm_ss", Locale.ENGLISH);
		LocalDateTime now = LocalDateTime.now();
		String timestamp = now.format(fmt);
		String fileName = "Card_Authorisation_Extract_" + timestamp + ".csv";
		AuthExtract authExtract = null;
		if (StringUtils.isNotBlank(receiptId)) {
			authExtract = this.authExtractRepository.findByReceiptIdAndCardId(receiptId, UUID.fromString(cardId));
			if (authExtract == null) {
				throw new BadRequestException("No auth exists with this combination");
			}
		}

		var csv = this.csvFileWriter.buildWorkingFilePath(fileName);
		Map<String, String> row = new LinkedHashMap<>();

		// Fields in the requested order with randomized values
		row.put("payment_request_id", RandomDataGenerator.uuidPrefix("REQ"));
		row.put("network_transaction_id", RandomDataGenerator.uuidPrefix("NET"));
		row.put("receipt_id", receiptId);
		row.put("original_auth_id", authExtract.getOriginalAuthRequestId());
		row.put("retrieval_reference_number", authExtract.getRetrievalReferenceNumber());
		row.put("stan", RandomDataGenerator.randomDigits(6));
		row.put("approval_code", RandomDataGenerator.randomAlphaNumeric(6));
		row.put("system_auth_time", RandomDataGenerator.timestampNow());
		row.put("merchant_auth_time", RandomDataGenerator.timestampPlusMinutes(-1));
		row.put("auth_expiry_time", RandomDataGenerator.timestampPlusMinutes(15));
		row.put("bin", RandomDataGenerator.randomDigits(6));
		row.put("ifi_id", RandomDataGenerator.randomDigits(6));
		row.put("ifi_name", "IFI_" + RandomDataGenerator.randomAlphaNumeric(4));
		row.put("card_id", cardId);
		row.put("card_sequence_number", String.valueOf(1 + Integer.parseInt(RandomDataGenerator.randomDigits(1))));
		row.put("payment_network", RandomDataGenerator.randomNetwork());
		row.put("masked_card_number", RandomDataGenerator.maskedCardNumber());
		row.put("mti", RandomDataGenerator.randomDigits(4));
		row.put("auth_amount", String.valueOf(authExtract.getAuthAmount()));
		row.put("auth_currency", "USD");
		row.put("billing_amount", String.valueOf(authExtract.getAuthAmount()));
		row.put("billing_currency", "USD");
		row.put("geo_indicator", RandomDataGenerator.randomAlphaNumeric(1));
		row.put("dcc_indicator", RandomDataGenerator.yesNo());
		row.put("pos_entry_mode", String.valueOf(90 + (int) (Math.random() * 9)));
		row.put("pos_transaction_code", RandomDataGenerator.randomDigits(2));
		row.put("pin_present_indicator", RandomDataGenerator.yesNo());
		row.put("pin_valid_indicator", RandomDataGenerator.yesNo());
		row.put("cvv2_present_indicator", RandomDataGenerator.yesNo());
		row.put("cvv2_valid_indicator", RandomDataGenerator.yesNo());
		row.put("ecom_indicator", RandomDataGenerator.yesNo());
		row.put("acs_correlation_id", RandomDataGenerator.uuid());
		row.put("eci_indicator", String.valueOf(1 + (int) (Math.random() * 9)));
		row.put("auth_action_code", "APPROVED");
		row.put("response_code", String.format("%02d", (int) (Math.random() * 100)));
		row.put("decline_reason",
				RandomDataGenerator.maybeEmpty("DECLINE_REASON_" + RandomDataGenerator.randomAlphaNumeric(4), 0.7));
		row.put("moto_indicator", RandomDataGenerator.yesNo());
		row.put("acquiring_institution_id", RandomDataGenerator.randomDigits(6));
		row.put("forwarding_institution_id", RandomDataGenerator.randomDigits(6));
		row.put("merchant_id", RandomDataGenerator.randomAlphaNumeric(8));
		row.put("merchant_terminal_id", RandomDataGenerator.randomAlphaNumeric(6));
		row.put("merchant_category_code", RandomDataGenerator.randomMerchantCategory());
		row.put("merchant_name", RandomDataGenerator.randomMerchantName());
		row.put("merchant_city", RandomDataGenerator.randomCity());
		row.put("merchant_state", RandomDataGenerator.randomState());
		row.put("merchant_country", RandomDataGenerator.randomCountry());
		row.put("payment_code", RandomDataGenerator.randomPaymentCode());
		row.put("payment_code_name", RandomDataGenerator.randomPaymentCodeName());
		row.put("channel_code", RandomDataGenerator.randomChannelCode());
		row.put("channel_code_name", RandomDataGenerator.randomChannelName());

		this.csvFileWriter.writeToCsvFile(csv, row);
		return fileName;
	}

}
