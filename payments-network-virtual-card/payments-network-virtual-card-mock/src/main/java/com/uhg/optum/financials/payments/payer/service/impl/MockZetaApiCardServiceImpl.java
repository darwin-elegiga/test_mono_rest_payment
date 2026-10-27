package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.CardDetailsDTO;
import com.uhg.optum.financials.payments.payer.dto.BulkAuthFileResponseDto;
import com.uhg.optum.financials.payments.payer.dto.BulkAuthWebhookResponseDto;
import com.uhg.optum.financials.payments.payer.dto.BulkClearFileResponseDto;
import com.uhg.optum.financials.payments.payer.dto.OrderDetails;
import com.uhg.optum.financials.payments.payer.dto.StatusReason;
import com.uhg.optum.financials.payments.payer.dto.SystemAttributes;
import com.uhg.optum.financials.payments.payer.dto.BinRange;
import com.uhg.optum.financials.payments.payer.dto.authevent.AuthorizationPayload;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.MockZetaApiCardService;
import com.uhg.optum.financials.payments.payer.utils.AuthExtractFieldMapper;
import com.uhg.optum.financials.payments.payer.utils.ClearExtractFieldMapper;
import com.uhg.optum.financials.payments.payer.utils.CsvFileWriter;
import com.uhg.optum.financials.payments.payer.utils.RandomDataGenerator;
import com.uhg.optum.financials.payments.payer.utils.WebhookDTOUtility;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestTemplate;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

import java.io.IOException;
import java.math.BigDecimal;
import java.math.RoundingMode;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.Collections;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;

@Service
public class MockZetaApiCardServiceImpl implements MockZetaApiCardService {

	private static final String DEBIT = "DEBIT";

	private static final String CREDIT = "CREDIT";

	@Autowired
	private VirtualCardsRepository virtualCardsRepository;

	@Autowired
	private AuthExtractFieldMapper authExtractFieldMapper;

	@Autowired
	private AuthExtractRepository authExtractRepository;

	@Autowired
	private ClearExtractFieldMapper clearExtractFieldMapper;

	@Autowired
	private CsvFileWriter csvFileWriter;

	@Value("${endpoint.virtualCard.webhook}")
	private String virtualCardWebhookEndpoint;

	private final RestTemplate restTemplate = new RestTemplate();

	@Override
	public CardDetailsDTO getCardDetails(UUID cardId) {
		CardDetailsDTO dto = new CardDetailsDTO();
		dto.setCguid(cardId.toString());
		dto.setCardID("MOCK_CARD_ID");
		dto.setCardHolderID("MOCK_HOLDER_ID");
		dto.setCrn("MOCK_CRN");
		dto.setCardType("VIRTUAL");
		dto.setMaskedPan("1234XXXXXXXX5678");
		dto.setCardStatus("ACTIVE");
		dto.setStatusReason(new StatusReason());
		dto.setOrderDetails(new OrderDetails());
		dto.setVectors(java.util.Arrays.asList("EMAIL", "SMS"));
		dto.setTenantAttributes(Collections.singletonMap("region", "US"));
		dto.setBinRange(new BinRange());
		dto.setTenantID(1001);
		dto.setTags(java.util.Arrays.asList("MOCK", "CARD"));
		dto.setValidFromMMYY("1025");
		dto.setExpiryMMYY("1030");
		dto.setSystemAttributes(new SystemAttributes());
		dto.setCardLifecycleID("MOCK_LIFECYCLE_ID");
		return dto;
	}

	@Override
	public String generateAuthExtract(String cardId, BigDecimal amount) throws IOException {
		String receiptId = this.authExtractFieldMapper.authFieldMapper(cardId, null);
		return receiptId;
	}

	@Override
	public AuthExtract getAuthExtractRecord(String receiptId) {
		return this.authExtractRepository.findByReceiptId(receiptId);
	}

	@Override
	public String uploadGenerateAuthExtract(String receiptId, String cardId) throws IOException {
		Optional<VirtualCard> virtualCard = this.virtualCardsRepository.findByCardGuid(UUID.fromString(cardId));
		if (virtualCard.isEmpty()) {
			throw new RuntimeException("Card not found for id: " + cardId);
		}
		String fileName = this.authExtractFieldMapper.authFieldMapper(cardId, receiptId);
		return fileName;
	}

	@Override
	public String generateClearExtract(String receiptId, String settlementIndicator, String cardId) throws IOException {
		return this.clearExtractFieldMapper.clearFieldMapper(receiptId, settlementIndicator, cardId);
	}

	@Override
	public BulkAuthWebhookResponseDto triggerAuthWebhooksFromDateTime(LocalDateTime startDateTime,
			int numberOfAuthEvents) {
		return triggerAuthWebhooksFromDateTime(startDateTime, numberOfAuthEvents, null);
	}

	@Override
	public BulkAuthWebhookResponseDto triggerAuthWebhooksFromDateTime(LocalDateTime startDateTime,
			int numberOfAuthEvents, List<UUID> cguids) {
		if (numberOfAuthEvents <= 0) {
			throw new BadRequestException("numberOfAuthEvents must be greater than 0");
		}

		LocalDateTime endDateTime = LocalDateTime.now();
		List<VirtualCard> cards = (cguids == null || cguids.isEmpty())
				? this.virtualCardsRepository.findByCreatedAtBetween(startDateTime, endDateTime)
				: this.virtualCardsRepository.findByCreatedAtBetweenAndCardGuidIn(startDateTime, endDateTime, cguids);

		BulkAuthWebhookResponseDto response = new BulkAuthWebhookResponseDto();
		response.setStartDateTime(startDateTime);
		response.setEndDateTime(endDateTime);
		response.setTotalCardsFound(cards.size());

		for (VirtualCard card : cards) {
			try {
				BigDecimal actualAmount = resolveActualAuthAmount(card);
				BigDecimal wholeActualAmount = validateSplitInputs(card, actualAmount, numberOfAuthEvents);
				if (wholeActualAmount == null) {
					response.getFailedCards().add(buildSplitValidationMessage(card, numberOfAuthEvents, actualAmount));
					continue;
				}
				List<BigDecimal> splitAmounts = splitWholeAmountForAuthEvents(wholeActualAmount, numberOfAuthEvents);

				String cardGuid = card.getCardGuid().toString();
				for (BigDecimal splitAmount : splitAmounts) {
					String receiptId = WebhookDTOUtility.generateReceiptId();
					ZetaWebhookDto<AuthorizationPayload> payload = WebhookDTOUtility.createAuthZetaWebhookDto(cardGuid,
							receiptId, splitAmount);
					postWebhook(payload);
					response.getReceiptIds().add(receiptId);
					response.getReceiptIdsByCardId()
						.computeIfAbsent(cardGuid, (key) -> new ArrayList<>())
						.add(receiptId);
				}
				response.setProcessedCards(response.getProcessedCards() + 1);
				response.setTotalWebhookCalls(response.getTotalWebhookCalls() + numberOfAuthEvents);
			}
			catch (Exception ex) {
				response.getFailedCards().add(card.getCardGuid() + " -> " + ex.getMessage());
			}
		}

		return response;
	}

	@Override
	public BulkAuthFileResponseDto generateAuthFileFromDb(LocalDateTime startDateTime) throws IOException {
		LocalDateTime endDateTime = LocalDateTime.now();
		List<AuthExtract> authExtracts = this.authExtractRepository
			.findByCreatedAtBetweenOrderByCreatedAtAsc(startDateTime, endDateTime);

		if (authExtracts.isEmpty()) {
			throw new RuntimeException("No auth_extract records found for the selected date range/card guids");
		}
		long uniqueCardCount = authExtracts.stream()
			.map(AuthExtract::getCardId)
			.filter(java.util.Objects::nonNull)
			.distinct()
			.count();

		DateTimeFormatter fmt = DateTimeFormatter.ofPattern("dd-MMM-yyyy_HH_mm_ss", Locale.ENGLISH);
		String timestamp = endDateTime.format(fmt);
		String fileName = "Card_Authorisation_Extract_" + timestamp + ".csv";
		var csvPath = this.csvFileWriter.buildWorkingFilePath(fileName);

		List<Map<String, String>> rows = authExtracts.stream().map(this::mapAuthExtractToCsvRow).toList();
		this.csvFileWriter.writeRowsToCsvFile(csvPath, rows);

		BulkAuthFileResponseDto response = new BulkAuthFileResponseDto();
		response.setStartDateTime(startDateTime);
		response.setEndDateTime(endDateTime);
		response.setTotalCardsFound((int) uniqueCardCount);
		response.setTotalAuthRecords(authExtracts.size());
		response.setFileName(fileName);
		return response;
	}

	@Override
	public BulkClearFileResponseDto generateClearFileFromDb(LocalDateTime startDateTime) throws IOException {
		LocalDateTime endDateTime = LocalDateTime.now();
		List<AuthExtract> authExtracts = this.authExtractRepository
			.findByCreatedAtBetweenOrderByCreatedAtAsc(startDateTime, endDateTime);

		if (authExtracts.isEmpty()) {
			throw new RuntimeException("No auth_extract records found for the selected date range/card guids");
		}

		Map<UUID, List<AuthExtract>> groupedByCardId = authExtracts.stream()
			.filter((authExtract) -> authExtract.getCardId() != null)
			.collect(java.util.stream.Collectors.groupingBy(AuthExtract::getCardId, LinkedHashMap::new,
					java.util.stream.Collectors.toList()));

		long uniqueCardCount = groupedByCardId.keySet().size();

		DateTimeFormatter fmt = DateTimeFormatter.ofPattern("dd-MMM-yyyy_HH_mm_ss", Locale.ENGLISH);
		String timestamp = endDateTime.format(fmt);
		String fileName = "Clearance_Extract_" + timestamp + ".csv";
		var csvPath = this.csvFileWriter.buildWorkingFilePath(fileName);

		List<Map<String, String>> rows = new ArrayList<>();
		int debitCount = 0;
		int creditCount = 0;

		for (List<AuthExtract> cardExtracts : groupedByCardId.values()) {
			int debitTransactionsForCard = calculateDebitCount(cardExtracts.size());
			for (int index = 0; index < cardExtracts.size(); index++) {
				String settlementIndicator = (index < debitTransactionsForCard) ? DEBIT : CREDIT;
				if (DEBIT.equals(settlementIndicator)) {
					debitCount++;
				}
				else {
					creditCount++;
				}
				rows.add(mapAuthExtractToClearCsvRow(cardExtracts.get(index), settlementIndicator, endDateTime));
			}
		}

		this.csvFileWriter.writeRowsToCsvFile(csvPath, rows);

		BulkClearFileResponseDto response = new BulkClearFileResponseDto();
		response.setStartDateTime(startDateTime);
		response.setEndDateTime(endDateTime);
		response.setTotalCardsFound((int) uniqueCardCount);
		response.setTotalClearRecords(rows.size());
		response.setTotalDebitTransactions(debitCount);
		response.setTotalCreditTransactions(creditCount);
		response.setFileName(fileName);
		return response;
	}

	private Map<String, String> mapAuthExtractToCsvRow(AuthExtract authExtract) {
		Map<String, String> row = new LinkedHashMap<>();
		row.put("payment_request_id", defaultString(authExtract.getPaymentRequestId()));
		row.put("network_transaction_id", defaultString(authExtract.getNetworkTransactionId()));
		row.put("receipt_id", defaultString(authExtract.getReceiptId()));
		row.put("original_auth_id", defaultString(authExtract.getOriginalAuthRequestId()));
		row.put("retrieval_reference_number", defaultString(authExtract.getRetrievalReferenceNumber()));
		row.put("stan", defaultString(authExtract.getStan()));
		row.put("approval_code", defaultString(authExtract.getApprovalCode()));
		row.put("system_auth_time",
				(authExtract.getSystemAuthTime() != null) ? authExtract.getSystemAuthTime().toString() : "");
		row.put("merchant_auth_time", defaultString(authExtract.getMerchantAuthTime()));
		row.put("auth_expiry_time", defaultString(authExtract.getAuthExpiryTime()));
		row.put("bin", defaultString(authExtract.getBin()));
		row.put("ifi_id", defaultString(authExtract.getIfiId()));
		row.put("ifi_name", defaultString(authExtract.getIfiName()));
		row.put("card_id", (authExtract.getCardId() != null) ? authExtract.getCardId().toString() : "");
		row.put("card_sequence_number", defaultString(authExtract.getCardSequenceNumber()));
		row.put("payment_network", defaultString(authExtract.getPaymentNetwork()));
		row.put("masked_card_number", defaultString(authExtract.getMaskedCardNumber()));
		row.put("mti", defaultString(authExtract.getMti()));
		row.put("auth_amount", toMinorUnits(authExtract.getAuthAmount()));
		row.put("auth_currency", defaultString(authExtract.getAuthCurrency()));
		row.put("billing_amount", toMinorUnits(authExtract.getBillingAmount()));
		row.put("billing_currency", defaultString(authExtract.getBillingCurrency()));
		row.put("geo_indicator", defaultString(authExtract.getGeoIndicator()));
		row.put("dcc_indicator", defaultString(authExtract.getDccIndicator()));
		row.put("pos_entry_mode", defaultString(authExtract.getPosEntryMode()));
		row.put("pos_transaction_code", defaultString(authExtract.getPosTransactionCode()));
		row.put("pin_present_indicator", defaultString(authExtract.getPinPresentIndicator()));
		row.put("pin_valid_indicator", defaultString(authExtract.getPinValidIndicator()));
		row.put("cvv2_present_indicator", defaultString(authExtract.getCvv2PresentIndicator()));
		row.put("cvv2_valid_indicator", defaultString(authExtract.getCvv2ValidIndicator()));
		row.put("ecom_indicator", defaultString(authExtract.getEcomIndicator()));
		row.put("acs_correlation_id", defaultString(authExtract.getAcsCorrelationId()));
		row.put("eci_indicator", defaultString(authExtract.getEciIndicator()));
		row.put("auth_action_code", defaultString(authExtract.getAuthActionCode()));
		row.put("response_code", defaultString(authExtract.getResponseCode()));
		row.put("decline_reason", defaultString(authExtract.getDeclineReason()));
		row.put("moto_indicator", defaultString(authExtract.getMotoIndicator()));
		row.put("acquiring_institution_id", defaultString(authExtract.getAcquiringInstitutionId()));
		row.put("forwarding_institution_id", defaultString(authExtract.getForwardingInstitutionId()));
		row.put("merchant_id", defaultString(authExtract.getMerchantId()));
		row.put("merchant_terminal_id", defaultString(authExtract.getMerchantTerminalId()));
		row.put("merchant_category_code", defaultString(authExtract.getMerchantCategoryCode()));
		row.put("merchant_name", defaultString(authExtract.getMerchantName()));
		row.put("merchant_city", defaultString(authExtract.getMerchantCity()));
		row.put("merchant_state", defaultString(authExtract.getMerchantState()));
		row.put("merchant_country", defaultString(authExtract.getMerchantCountry()));
		row.put("payment_code", defaultString(authExtract.getPaymentCode()));
		row.put("payment_code_name", defaultString(authExtract.getPaymentCodeName()));
		row.put("channel_code", defaultString(authExtract.getChannelCode()));
		row.put("channel_code_name", defaultString(authExtract.getChannelCodeName()));
		return row;
	}

	private BigDecimal resolveActualAuthAmount(VirtualCard card) {
		if (card.getCurrentBalance() != null && card.getCurrentBalance().signum() > 0) {
			return card.getCurrentBalance();
		}
		if (card.getAvailableBalance() != null && card.getAvailableBalance().signum() > 0) {
			return card.getAvailableBalance();
		}
		return card.getInitialBalance();
	}

	private BigDecimal validateSplitInputs(VirtualCard card, BigDecimal actualAmount, int numberOfAuthEvents) {
		if (actualAmount == null || actualAmount.signum() <= 0) {
			throw new BadRequestException("Invalid amount for cardGuid " + card.getCardGuid());
		}

		BigDecimal wholeActualAmount = actualAmount.setScale(0, RoundingMode.DOWN);
		if (wholeActualAmount.compareTo(BigDecimal.valueOf(numberOfAuthEvents + 1L)) < 0) {
			return null;
		}
		return wholeActualAmount;
	}

	private String buildSplitValidationMessage(VirtualCard card, int numberOfAuthEvents, BigDecimal actualAmount) {
		return "Skipped cardGuid " + card.getCardGuid() + " because numberOfAuthEvents=" + numberOfAuthEvents
				+ " is too high for amount=" + actualAmount + ". numberOfAuthEvents must be <= amount-1.";
	}

	private List<BigDecimal> splitWholeAmountForAuthEvents(BigDecimal wholeActualAmount, int numberOfAuthEvents) {
		BigDecimal distributable = wholeActualAmount.subtract(BigDecimal.ONE);
		BigDecimal count = BigDecimal.valueOf(numberOfAuthEvents);
		BigDecimal base = distributable.divide(count, 0, RoundingMode.DOWN);
		int remainder = distributable.remainder(count).intValue();

		List<BigDecimal> amounts = new ArrayList<>();
		for (int i = 0; i < numberOfAuthEvents; i++) {
			BigDecimal amount = base.add((i < remainder) ? BigDecimal.ONE : BigDecimal.ZERO);
			if (amount.signum() <= 0) {
				throw new BadRequestException("Unable to split amount into positive integers.");
			}
			amounts.add(amount);
		}

		BigDecimal sum = amounts.stream().reduce(BigDecimal.ZERO, BigDecimal::add);
		if (sum.compareTo(wholeActualAmount) >= 0) {
			throw new BadRequestException("Calculated split amounts are not strictly less than actual amount.");
		}

		return amounts;
	}

	private void postWebhook(ZetaWebhookDto<AuthorizationPayload> payload) {
		HttpHeaders headers = new HttpHeaders();
		headers.setContentType(MediaType.APPLICATION_JSON);
		String authorizationHeader = currentAuthorizationHeader();
		if (authorizationHeader != null && !authorizationHeader.isBlank()) {
			headers.set(HttpHeaders.AUTHORIZATION, authorizationHeader);
		}
		HttpEntity<ZetaWebhookDto<AuthorizationPayload>> request = new HttpEntity<>(payload, headers);
		ResponseEntity<String> result = this.restTemplate.postForEntity(this.virtualCardWebhookEndpoint, request,
				String.class);
		if (!result.getStatusCode().is2xxSuccessful()) {
			throw new RuntimeException("Webhook call failed with status: " + result.getStatusCode());
		}
	}

	private String currentAuthorizationHeader() {
		if (RequestContextHolder.getRequestAttributes() instanceof ServletRequestAttributes attributes) {
			return attributes.getRequest().getHeader(HttpHeaders.AUTHORIZATION);
		}
		return null;
	}

	private String defaultString(String value) {
		return (value != null) ? value : "";
	}

	private String toMinorUnits(BigDecimal amount) {
		if (amount == null) {
			return "";
		}
		return amount.movePointRight(2).setScale(0, RoundingMode.DOWN).toPlainString();
	}

	private int calculateDebitCount(int totalTransactions) {
		if (totalTransactions <= 1) {
			return totalTransactions;
		}
		int debitCount = (int) Math.floor(totalTransactions * 0.8d);
		debitCount = Math.max(1, debitCount);
		if (debitCount >= totalTransactions) {
			return totalTransactions - 1;
		}
		return debitCount;
	}

	private Map<String, String> mapAuthExtractToClearCsvRow(AuthExtract authExtract, String settlementIndicator,
			LocalDateTime now) {
		String batchTs = now.format(DateTimeFormatter.ofPattern("yyMMddHHmmss", Locale.ENGLISH));
		String settlementDate = now.format(DateTimeFormatter.ofPattern("yyMMdd", Locale.ENGLISH));
		String purchaseDate = now.format(DateTimeFormatter.ofPattern("MMdd", Locale.ENGLISH));
		String clearingDate = now.format(DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss", Locale.ENGLISH));
		BigDecimal clearingAmount = (authExtract.getAuthAmount() != null) ? authExtract.getAuthAmount()
				: BigDecimal.TEN;
		String clearingAmountInMinorUnits = toMinorUnits(clearingAmount);
		String[] clearingInfo = this.clearExtractFieldMapper.getClearingInfo(settlementIndicator);

		Map<String, String> row = new LinkedHashMap<>();
		row.put("file_id", withFallbackAndMax("001" + settlementDate + "000SIM" + RandomDataGenerator.randomDigits(10),
				"001" + settlementDate + "000SIM" + RandomDataGenerator.randomDigits(10), 100));
		row.put("file_name", withFallbackAndMax(
				"MASTERCARD_CLEARANCE_REPORT_SIM_" + batchTs + "/MCI.AR.T112.SIM.301117." + batchTs + ".A053",
				"MASTERCARD_CLEARANCE_REPORT_SIM_" + batchTs + "/MCI.AR.T112.SIM.301117." + batchTs + ".A053", 200));
		row.put("message_sequence_number",
				withFallbackAndMax(RandomDataGenerator.randomDigits(8), RandomDataGenerator.randomDigits(8), 200));
		row.put("network", withFallbackAndMax(authExtract.getPaymentNetwork(), "MASTERCARD", 20));
		row.put("network_transaction_id",
				withFallbackAndMax(authExtract.getNetworkTransactionId(), RandomDataGenerator.uuidPrefix("NET"), 100));
		row.put("receipt_id", capped(defaultString(authExtract.getReceiptId()), 20));
		row.put("original_auth_id", capped(defaultString(authExtract.getOriginalAuthRequestId()), 256));
		row.put("acquirer_institution_id", capped(withFallback(authExtract.getAcquiringInstitutionId(), "968500"), 6));
		row.put("ifi_id", withFallbackAndMax(authExtract.getIfiId(), "600710", 10));
		row.put("ifi_name", withFallbackAndMax(authExtract.getIfiName(), "optumbankdemo", 40));
		row.put("bin", capped(withFallback(authExtract.getBin(), "111111"), 10));
		row.put("card_id", capped((authExtract.getCardId() != null) ? authExtract.getCardId().toString() : "", 36));
		row.put("masked_pan_number", capped(withFallback(authExtract.getMaskedCardNumber(), "111111-xxxxxx-0000"), 19));
		row.put("card_status", capped("ENABLED", 10));
		row.put("account_holder_id", capped(RandomDataGenerator.uuid(), 40));
		row.put("settlement_date", settlementDate);
		row.put("settlement_indicator", capped(settlementIndicator, 8));
		row.put("approval_code", capped(CREDIT.equals(settlementIndicator) ? "0"
				: withFallback(authExtract.getApprovalCode(), RandomDataGenerator.randomDigits(6)), 6));
		row.put("retrieval_reference_number", capped(
				withFallback(authExtract.getRetrievalReferenceNumber(), RandomDataGenerator.randomDigits(12)), 50));
		row.put("acquirer_reference_number", capped(RandomDataGenerator.randomDigits(23), 50));
		row.put("clearing_status", capped("SUCCESS", 50));
		row.put("clearing_type", capped(clearingInfo[0], 50));
		row.put("clearing_sub_category", capped(clearingInfo[1], 64));
		row.put("purchase_date", purchaseDate);
		row.put("clearing_date", clearingDate);
		row.put("clearing_amount", clearingAmountInMinorUnits);
		row.put("clearing_currency", "840");
		row.put("billing_amount", clearingAmountInMinorUnits);
		row.put("billing_currency", "840");
		row.put("failure_reason", "");
		row.put("geo_indicator", capped(withFallback(authExtract.getGeoIndicator(), "INTERNATIONAL"), 13));
		row.put("merchant_id", capped(defaultString(authExtract.getMerchantId()), 200));
		row.put("merchant_name", capped(defaultString(authExtract.getMerchantName()), 40));
		row.put("merchant_terminal_id", capped(withFallback(authExtract.getMerchantTerminalId(), "TID001"), 8));
		row.put("merchant_category_code", capped(withFallback(authExtract.getMerchantCategoryCode(), "5611"), 4));
		row.put("merchant_city", capped(defaultString(authExtract.getMerchantCity()), 40));
		row.put("merchant_country_code", capped(defaultString(authExtract.getMerchantCountry()), 3));
		row.put("interchange_fee_amount", "");
		row.put("interchange_fee_sign", "");
		row.put("interchange_fee_currency", "");
		row.put("payment_code", capped(withFallback(authExtract.getPaymentCode(), "PMCUSZZ112910272"), 20));
		row.put("payment_code_name",
				capped(withFallback(authExtract.getPaymentCodeName(), "INT_ECOM_CIT_RECURRING_PREAUTH"), 50));
		row.put("channel_code", capped(withFallback(authExtract.getChannelCode(), "CHNUSZZ1129"), 20));
		row.put("channel_code_name",
				capped(withFallback(authExtract.getChannelCodeName(), "MC B2B US Offus Channel"), 50));
		row.put("mti", withFallbackAndMax(authExtract.getMti(), "1240", 4));
		row.put("processing_code", capped("200000", 10));
		row.put("function_code", capped("200", 10));
		return row;
	}

	private String withFallbackAndMax(String value, String fallback, int maxLength) {
		return capped(withFallback(value, fallback), maxLength);
	}

	private String withFallback(String value, String fallback) {
		return (value == null || value.isBlank()) ? fallback : value;
	}

	private String capped(String value, int maxLength) {
		String safe = defaultString(value);
		if (safe.length() <= maxLength) {
			return safe;
		}
		return safe.substring(0, maxLength);
	}

}
