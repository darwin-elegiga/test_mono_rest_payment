package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.BulkAuthFileResponseDto;
import com.uhg.optum.financials.payments.payer.dto.BulkAuthWebhookResponseDto;
import com.uhg.optum.financials.payments.payer.dto.BulkClearFileResponseDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.utils.AuthExtractFieldMapper;
import com.uhg.optum.financials.payments.payer.utils.ClearExtractFieldMapper;
import com.uhg.optum.financials.payments.payer.utils.CsvFileWriter;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Captor;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.math.BigDecimal;
import java.nio.file.Path;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.OffsetDateTime;
import java.lang.reflect.Field;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

import static org.assertj.core.api.BDDAssertions.then;
import static org.assertj.core.api.BDDAssertions.thenThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.ArgumentMatchers.isA;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.never;
import static org.springframework.test.web.client.ExpectedCount.once;
import static org.springframework.test.web.client.ExpectedCount.times;
import static org.springframework.test.web.client.match.MockRestRequestMatchers.method;
import static org.springframework.test.web.client.match.MockRestRequestMatchers.requestTo;
import static org.springframework.test.web.client.response.MockRestResponseCreators.withStatus;
import static org.springframework.test.web.client.response.MockRestResponseCreators.withSuccess;

import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.test.util.ReflectionTestUtils;
import org.springframework.test.web.client.MockRestServiceServer;
import org.springframework.web.client.RestTemplate;

@ExtendWith(MockitoExtension.class)
class MockZetaApiCardServiceImplTests {

	@Mock
	private VirtualCardsRepository virtualCardsRepository;

	@Mock
	private AuthExtractFieldMapper authExtractFieldMapper;

	@Mock
	private AuthExtractRepository authExtractRepository;

	@Mock
	private ClearExtractFieldMapper clearExtractFieldMapper;

	@Mock
	private CsvFileWriter csvFileWriter;

	@InjectMocks
	private MockZetaApiCardServiceImpl service;

	@Captor
	private ArgumentCaptor<List<Map<String, String>>> rowsCaptor;

	@Test
	void generateAuthFileFromDb_shouldGenerateCsvAndReturnResponse() throws Exception {
		LocalDateTime startDateTime = LocalDateTime.now().minusMinutes(2);
		UUID sharedCardId = UUID.randomUUID();

		AuthExtract first = new AuthExtract();
		first.setCardId(sharedCardId);
		first.setReceiptId("R1");
		first.setPaymentRequestId("PR1");
		first.setOriginalAuthRequestId("OAR1");
		first.setSystemAuthTime(OffsetDateTime.parse("2026-04-10T10:00:00+00:00"));
		first.setAuthAmount(new BigDecimal("12.34"));

		AuthExtract second = new AuthExtract();
		second.setCardId(sharedCardId);
		second.setReceiptId("R2");
		second.setAuthAmount(new BigDecimal("5.00"));

		AuthExtract third = new AuthExtract();
		third.setCardId(null);
		third.setReceiptId("R3");

		List<AuthExtract> extracts = List.of(first, second, third);

		given(this.authExtractRepository.findByCreatedAtBetweenOrderByCreatedAtAsc(eq(startDateTime),
				any(LocalDateTime.class)))
			.willReturn(extracts);
		given(this.csvFileWriter.buildWorkingFilePath(anyString()))
			.willAnswer((invocation) -> Path.of(invocation.getArgument(0, String.class)));

		BulkAuthFileResponseDto response = this.service.generateAuthFileFromDb(startDateTime);

		then(response.getStartDateTime()).isEqualTo(startDateTime);
		then(response.getEndDateTime()).isNotNull();
		then(response.getEndDateTime()).isAfterOrEqualTo(startDateTime);
		then(response.getTotalCardsFound()).isEqualTo(1);
		then(response.getTotalAuthRecords()).isEqualTo(3);
		then(response.getFileName())
			.matches("Card_Authorisation_Extract_\\d{2}-[A-Za-z]{3}-\\d{4}_\\d{2}_\\d{2}_\\d{2}\\.csv");

		BDDMockito.then(this.csvFileWriter).should().buildWorkingFilePath(response.getFileName());
		BDDMockito.then(this.csvFileWriter).should().writeRowsToCsvFile(any(Path.class), this.rowsCaptor.capture());

		List<Map<String, String>> writtenRows = this.rowsCaptor.getValue();
		then(writtenRows).hasSize(3);
		then(writtenRows.get(0).get("receipt_id")).isEqualTo("R1");
		then(writtenRows.get(0).get("payment_request_id")).isEqualTo("PR1");
		then(writtenRows.get(0).get("auth_amount")).isEqualTo("1234");
		then(writtenRows.get(2).get("card_id")).isEmpty();
	}

	@Test
	void generateAuthFileFromDb_shouldThrowWhenNoRecordsFound() throws Exception {
		LocalDateTime startDateTime = LocalDateTime.of(2026, 4, 10, 0, 0);
		given(this.authExtractRepository.findByCreatedAtBetweenOrderByCreatedAtAsc(eq(startDateTime),
				any(LocalDateTime.class)))
			.willReturn(List.of());

		thenThrownBy(() -> this.service.generateAuthFileFromDb(startDateTime)).isInstanceOf(RuntimeException.class)
			.hasMessage("No auth_extract records found for the selected date range/card guids");

		BDDMockito.then(this.csvFileWriter).should(never()).buildWorkingFilePath(anyString());
		BDDMockito.then(this.csvFileWriter)
			.should(never())
			.writeRowsToCsvFile(isA(Path.class), org.mockito.ArgumentMatchers.<List<Map<String, String>>>any());
	}

	@Test
	void generateClearFileFromDb_shouldGenerateCsvAndReturnResponseWith80_20Split() throws Exception {
		LocalDateTime startDateTime = LocalDateTime.now().minusMinutes(5);
		UUID cardOne = UUID.randomUUID();
		UUID cardTwo = UUID.randomUUID();

		List<AuthExtract> extracts = List.of(buildAuthExtract(cardOne, "R1"), buildAuthExtract(cardOne, "R2"),
				buildAuthExtract(cardOne, "R3"), buildAuthExtract(cardOne, "R4"), buildAuthExtract(cardOne, "R5"),
				buildAuthExtract(cardTwo, "R6"), buildAuthExtract(cardTwo, "R7"), buildAuthExtract(cardTwo, "R8"),
				buildAuthExtract(cardTwo, "R9"), buildAuthExtract(cardTwo, "R10"));

		given(this.authExtractRepository.findByCreatedAtBetweenOrderByCreatedAtAsc(eq(startDateTime),
				any(LocalDateTime.class)))
			.willReturn(extracts);
		given(this.csvFileWriter.buildWorkingFilePath(anyString()))
			.willAnswer((invocation) -> Path.of(invocation.getArgument(0, String.class)));
		given(this.clearExtractFieldMapper.getClearingInfo("DEBIT"))
			.willReturn(new String[] { "FIRST_PRESENTMENT", "MATCHED_AUTH" });
		given(this.clearExtractFieldMapper.getClearingInfo("CREDIT"))
			.willReturn(new String[] { "MERCHANDISE_RETURN", "MATCHED_REFUNDS" });

		BulkClearFileResponseDto response = this.service.generateClearFileFromDb(startDateTime);

		then(response.getStartDateTime()).isEqualTo(startDateTime);
		then(response.getEndDateTime()).isNotNull();
		then(response.getTotalCardsFound()).isEqualTo(2);
		then(response.getTotalClearRecords()).isEqualTo(10);
		then(response.getTotalDebitTransactions()).isEqualTo(8);
		then(response.getTotalCreditTransactions()).isEqualTo(2);
		then(response.getFileName()).matches("Clearance_Extract_\\d{2}-[A-Za-z]{3}-\\d{4}_\\d{2}_\\d{2}_\\d{2}\\.csv");

		BDDMockito.then(this.csvFileWriter).should().buildWorkingFilePath(response.getFileName());
		BDDMockito.then(this.csvFileWriter).should().writeRowsToCsvFile(any(Path.class), this.rowsCaptor.capture());

		List<Map<String, String>> writtenRows = this.rowsCaptor.getValue();
		then(writtenRows).hasSize(10);
		long debitCount = writtenRows.stream().filter((row) -> "DEBIT".equals(row.get("settlement_indicator"))).count();
		long creditCount = writtenRows.stream()
			.filter((row) -> "CREDIT".equals(row.get("settlement_indicator")))
			.count();
		then(debitCount).isEqualTo(8);
		then(creditCount).isEqualTo(2);
		then(writtenRows).allSatisfy((row) -> {
			then(row.get("clearing_amount")).isEqualTo("1000");
			then(row.get("billing_amount")).isEqualTo("1000");
		});

		Map<String, Integer> maxLengths = new LinkedHashMap<>();
		maxLengths.put("file_id", 100);
		maxLengths.put("file_name", 200);
		maxLengths.put("message_sequence_number", 200);
		maxLengths.put("original_auth_id", 256);
		maxLengths.put("network", 20);
		maxLengths.put("network_transaction_id", 100);
		maxLengths.put("receipt_id", 20);
		maxLengths.put("acquirer_institution_id", 6);
		maxLengths.put("ifi_id", 10);
		maxLengths.put("ifi_name", 40);
		maxLengths.put("bin", 10);
		maxLengths.put("card_id", 36);
		maxLengths.put("masked_pan_number", 19);
		maxLengths.put("card_status", 10);
		maxLengths.put("account_holder_id", 40);
		maxLengths.put("settlement_date", 6);
		maxLengths.put("settlement_indicator", 8);
		maxLengths.put("approval_code", 6);
		maxLengths.put("retrieval_reference_number", 50);
		maxLengths.put("acquirer_reference_number", 50);
		maxLengths.put("clearing_status", 50);
		maxLengths.put("clearing_type", 50);
		maxLengths.put("clearing_sub_category", 64);
		maxLengths.put("purchase_date", 12);
		maxLengths.put("clearing_currency", 3);
		maxLengths.put("billing_currency", 3);
		maxLengths.put("failure_reason", 200);
		maxLengths.put("geo_indicator", 13);
		maxLengths.put("merchant_id", 200);
		maxLengths.put("merchant_name", 40);
		maxLengths.put("merchant_terminal_id", 8);
		maxLengths.put("merchant_category_code", 4);
		maxLengths.put("merchant_city", 40);
		maxLengths.put("merchant_country_code", 3);
		maxLengths.put("interchange_fee_sign", 1);
		maxLengths.put("interchange_fee_currency", 3);
		maxLengths.put("payment_code", 20);
		maxLengths.put("payment_code_name", 50);
		maxLengths.put("channel_code", 20);
		maxLengths.put("channel_code_name", 50);
		maxLengths.put("mti", 4);
		maxLengths.put("processing_code", 10);
		maxLengths.put("function_code", 10);

		for (Map<String, String> row : writtenRows) {
			then(row.get("file_id")).isNotBlank();
			then(row.get("file_name")).isNotBlank();
			then(row.get("message_sequence_number")).isNotBlank();
			then(row.get("network")).isNotBlank();
			then(row.get("ifi_id")).isNotBlank();
			then(row.get("ifi_name")).isNotBlank();
			then(row.get("acquirer_reference_number")).isNotBlank();
			then(row.get("clearing_status")).isNotBlank();
			then(row.get("clearing_type")).isNotBlank();
			then(row.get("clearing_date")).isNotBlank();
			then(row.get("mti")).isNotBlank();

			for (Map.Entry<String, Integer> max : maxLengths.entrySet()) {
				String value = row.get(max.getKey());
				then(value).isNotNull();
				then(value.length()).isLessThanOrEqualTo(max.getValue());
			}
		}
	}

	@Test
	void generateClearFileFromDb_shouldWriteClearAmountsInMinorUnits() throws Exception {
		LocalDateTime startDateTime = LocalDateTime.now().minusMinutes(5);
		UUID cardId = UUID.randomUUID();
		AuthExtract authExtract = buildAuthExtract(cardId, "R1");
		authExtract.setAuthAmount(new BigDecimal("20.00"));

		given(this.authExtractRepository.findByCreatedAtBetweenOrderByCreatedAtAsc(eq(startDateTime),
				any(LocalDateTime.class)))
			.willReturn(List.of(authExtract));
		given(this.csvFileWriter.buildWorkingFilePath(anyString()))
			.willAnswer((invocation) -> Path.of(invocation.getArgument(0, String.class)));
		given(this.clearExtractFieldMapper.getClearingInfo("DEBIT"))
			.willReturn(new String[] { "FIRST_PRESENTMENT", "MATCHED_AUTH" });

		this.service.generateClearFileFromDb(startDateTime);

		BDDMockito.then(this.csvFileWriter).should().writeRowsToCsvFile(any(Path.class), this.rowsCaptor.capture());
		List<Map<String, String>> writtenRows = this.rowsCaptor.getValue();
		then(writtenRows).hasSize(1);
		then(writtenRows.getFirst().get("clearing_amount")).isEqualTo("2000");
		then(writtenRows.getFirst().get("billing_amount")).isEqualTo("2000");
	}

	@Test
	void generateClearFileFromDb_shouldThrowWhenNoRecordsFound() throws Exception {
		LocalDateTime startDateTime = LocalDateTime.of(2026, 4, 10, 0, 0);
		given(this.authExtractRepository.findByCreatedAtBetweenOrderByCreatedAtAsc(eq(startDateTime),
				any(LocalDateTime.class)))
			.willReturn(List.of());

		thenThrownBy(() -> this.service.generateClearFileFromDb(startDateTime)).isInstanceOf(RuntimeException.class)
			.hasMessage("No auth_extract records found for the selected date range/card guids");

		BDDMockito.then(this.csvFileWriter).should(never()).buildWorkingFilePath(anyString());
		BDDMockito.then(this.csvFileWriter)
			.should(never())
			.writeRowsToCsvFile(isA(Path.class), org.mockito.ArgumentMatchers.<List<Map<String, String>>>any());
	}

	@Test
	void triggerAuthWebhooksFromDateTime_shouldThrowWhenEventCountIsNotPositive() {
		LocalDateTime startDateTime = LocalDateTime.of(2026, 4, 10, 0, 0);

		thenThrownBy(() -> this.service.triggerAuthWebhooksFromDateTime(startDateTime, 0))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("numberOfAuthEvents must be greater than 0");
	}

	@Test
	void triggerAuthWebhooksFromDateTime_shouldProcessCardAndGenerateReceipts() throws Exception {
		LocalDateTime startDateTime = LocalDateTime.of(2026, 4, 10, 0, 0);
		VirtualCard card = buildCard(new BigDecimal("11.90"));
		given(this.virtualCardsRepository.findByCreatedAtBetween(eq(startDateTime), any(LocalDateTime.class)))
			.willReturn(List.of(card));

		ReflectionTestUtils.setField(this.service, "virtualCardWebhookEndpoint", "http://localhost/webhook");
		MockRestServiceServer server = MockRestServiceServer.bindTo(getRestTemplate()).build();
		server.expect(times(2), requestTo("http://localhost/webhook"))
			.andExpect(method(HttpMethod.POST))
			.andRespond(withSuccess("ok", MediaType.APPLICATION_JSON));

		BulkAuthWebhookResponseDto response = this.service.triggerAuthWebhooksFromDateTime(startDateTime, 2);

		then(response.getStartDateTime()).isEqualTo(startDateTime);
		then(response.getEndDateTime()).isNotNull();
		then(response.getTotalCardsFound()).isEqualTo(1);
		then(response.getProcessedCards()).isEqualTo(1);
		then(response.getTotalWebhookCalls()).isEqualTo(2);
		then(response.getReceiptIds()).hasSize(2);
		then(response.getReceiptIdsByCardId()).hasSize(1);
		then(response.getReceiptIdsByCardId()).containsKey(card.getCardGuid().toString());
		then(response.getReceiptIdsByCardId().get(card.getCardGuid().toString())).hasSize(2);
		then(response.getReceiptIdsByCardId().get(card.getCardGuid().toString()))
			.containsExactlyElementsOf(response.getReceiptIds());
		then(response.getFailedCards()).isEmpty();
		server.verify();
	}

	@Test
	void triggerAuthWebhooksFromDateTime_shouldSkipCardWhenAmountIsTooLowForSplit() {
		LocalDateTime startDateTime = LocalDateTime.of(2026, 4, 10, 0, 0);
		VirtualCard card = buildCard(new BigDecimal("2.00"));
		given(this.virtualCardsRepository.findByCreatedAtBetween(eq(startDateTime), any(LocalDateTime.class)))
			.willReturn(List.of(card));

		BulkAuthWebhookResponseDto response = this.service.triggerAuthWebhooksFromDateTime(startDateTime, 3);

		then(response.getTotalCardsFound()).isEqualTo(1);
		then(response.getProcessedCards()).isZero();
		then(response.getTotalWebhookCalls()).isZero();
		then(response.getReceiptIds()).isEmpty();
		then(response.getReceiptIdsByCardId()).isEmpty();
		then(response.getFailedCards()).hasSize(1);
		then(response.getFailedCards().get(0)).contains("Skipped cardGuid " + card.getCardGuid());
	}

	@Test
	void triggerAuthWebhooksFromDateTime_shouldRecordFailureWhenWebhookReturnsNon2xx() throws Exception {
		LocalDateTime startDateTime = LocalDateTime.of(2026, 4, 10, 0, 0);
		VirtualCard card = buildCard(new BigDecimal("12.00"));
		given(this.virtualCardsRepository.findByCreatedAtBetween(eq(startDateTime), any(LocalDateTime.class)))
			.willReturn(List.of(card));

		ReflectionTestUtils.setField(this.service, "virtualCardWebhookEndpoint", "http://localhost/webhook");
		MockRestServiceServer server = MockRestServiceServer.bindTo(getRestTemplate()).build();
		server.expect(once(), requestTo("http://localhost/webhook"))
			.andExpect(method(HttpMethod.POST))
			.andRespond(withStatus(HttpStatus.INTERNAL_SERVER_ERROR));

		BulkAuthWebhookResponseDto response = this.service.triggerAuthWebhooksFromDateTime(startDateTime, 2);

		then(response.getTotalCardsFound()).isEqualTo(1);
		then(response.getProcessedCards()).isZero();
		then(response.getTotalWebhookCalls()).isZero();
		then(response.getReceiptIds()).isEmpty();
		then(response.getReceiptIdsByCardId()).isEmpty();
		then(response.getFailedCards()).hasSize(1);
		then(response.getFailedCards().get(0)).contains(card.getCardGuid().toString());
		server.verify();
	}

	@Test
	void triggerAuthWebhooksFromDateTime_shouldFilterByCardGuidsWhenProvided() throws Exception {
		LocalDateTime startDateTime = LocalDateTime.of(2026, 4, 10, 0, 0);
		VirtualCard card = buildCard(new BigDecimal("12.00"));
		List<UUID> cguids = List.of(card.getCardGuid());
		given(this.virtualCardsRepository.findByCreatedAtBetweenAndCardGuidIn(eq(startDateTime),
				any(LocalDateTime.class), eq(cguids)))
			.willReturn(List.of(card));

		ReflectionTestUtils.setField(this.service, "virtualCardWebhookEndpoint", "http://localhost/webhook");
		MockRestServiceServer server = MockRestServiceServer.bindTo(getRestTemplate()).build();
		server.expect(once(), requestTo("http://localhost/webhook"))
			.andExpect(method(HttpMethod.POST))
			.andRespond(withSuccess("ok", MediaType.APPLICATION_JSON));

		BulkAuthWebhookResponseDto response = this.service.triggerAuthWebhooksFromDateTime(startDateTime, 1, cguids);

		then(response.getTotalCardsFound()).isEqualTo(1);
		then(response.getProcessedCards()).isEqualTo(1);
		then(response.getTotalWebhookCalls()).isEqualTo(1);
		then(response.getReceiptIds()).hasSize(1);
		then(response.getReceiptIdsByCardId()).containsKey(card.getCardGuid().toString());

		BDDMockito.then(this.virtualCardsRepository)
			.should()
			.findByCreatedAtBetweenAndCardGuidIn(eq(startDateTime), any(LocalDateTime.class), eq(cguids));
		BDDMockito.then(this.virtualCardsRepository)
			.should(never())
			.findByCreatedAtBetween(eq(startDateTime), any(LocalDateTime.class));
		server.verify();
	}

	private VirtualCard buildCard(BigDecimal currentBalance) {
		VirtualCard card = new VirtualCard();
		card.setCardGuid(UUID.randomUUID());
		card.setCurrentBalance(currentBalance);
		card.setAvailableBalance(BigDecimal.ZERO);
		card.setInitialBalance(currentBalance);
		card.setCreatedAt(LocalDateTime.now().minusMinutes(1));
		card.setExpiryDate(LocalDate.now().plusMonths(6));
		card.setPaymentId("PAYMENT-ID");
		card.setStatus("ACTIVE");
		return card;
	}

	private RestTemplate getRestTemplate() throws Exception {
		Field restTemplateField = MockZetaApiCardServiceImpl.class.getDeclaredField("restTemplate");
		restTemplateField.setAccessible(true);
		return (RestTemplate) restTemplateField.get(this.service);
	}

	private AuthExtract buildAuthExtract(UUID cardId, String receiptId) {
		AuthExtract authExtract = new AuthExtract();
		authExtract.setCardId(cardId);
		authExtract.setReceiptId(receiptId);
		authExtract.setOriginalAuthRequestId("AUTH-" + receiptId + "-EXTENDED-VALUE-FOR-COLUMN-LIMIT-CHECK-0123456789");
		authExtract.setAuthAmount(new BigDecimal("10.00"));
		authExtract.setPaymentNetwork("MASTERCARD-VERY-LONG-NETWORK");
		authExtract.setAcquiringInstitutionId("1234567");
		authExtract.setIfiId("IFI-TOO-LONG-123");
		authExtract.setIfiName("IFI-NAME-WITH-MORE-THAN-FORTY-CHARS-1234567890");
		authExtract.setBin("1234567890123");
		authExtract.setMaskedCardNumber("12345678901234567890123");
		authExtract.setApprovalCode("123456789");
		authExtract.setRetrievalReferenceNumber("12345678901234567890123456789012345678901234567890EXTRA");
		authExtract.setGeoIndicator("INTERNATIONAL-EXTRA");
		authExtract.setMerchantId("MERCHANT-ID-EXTRA-LONG-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
		authExtract.setMerchantName("MERCHANT-NAME-EXTRA-LONG-XXXXXXXXXXXXXXXXXXXXXXXX");
		authExtract.setMerchantTerminalId("TERMINAL-TOO-LONG");
		authExtract.setMerchantCategoryCode("123456");
		authExtract.setMerchantCity("CITY-NAME-EXTRA-LONG-XXXXXXXXXXXXXXXXXXXXXXXXXXX");
		authExtract.setMerchantCountry("INDIA");
		authExtract.setPaymentCode("PAYMENT-CODE-EXTRA-LONG-123456");
		authExtract.setPaymentCodeName("PAYMENT-CODE-NAME-EXTRA-LONG-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
		authExtract.setChannelCode("CHANNEL-CODE-EXTRA-LONG-123456");
		authExtract.setChannelCodeName("CHANNEL-CODE-NAME-EXTRA-LONG-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
		authExtract.setMti("12405");
		authExtract.setSystemAuthTime(OffsetDateTime.now());
		return authExtract;
	}

}
