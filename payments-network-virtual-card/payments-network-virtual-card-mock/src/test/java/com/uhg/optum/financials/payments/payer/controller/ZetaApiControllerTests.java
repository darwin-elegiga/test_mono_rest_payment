package com.uhg.optum.financials.payments.payer.controller;

import static org.assertj.core.api.BDDAssertions.then;
import static org.mockito.BDDMockito.given;

import com.uhg.optum.financials.payments.payer.dto.BulkAuthFileResponseDto;
import com.uhg.optum.financials.payments.payer.dto.BulkAuthWebhookResponseDto;
import com.uhg.optum.financials.payments.payer.dto.BulkClearFileResponseDto;
import com.uhg.optum.financials.payments.payer.dto.CardDetailsDTO;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.service.MockZetaApiCardService;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.ResponseEntity;
import org.springframework.web.server.ResponseStatusException;

import java.io.IOException;
import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.List;
import java.util.UUID;

@ExtendWith(MockitoExtension.class)
class ZetaApiControllerTests {

	@Mock
	private MockZetaApiCardService cardDetailsService;

	@InjectMocks
	private ZetaApiController zetaApiController;

	@Test
	void testGetCardDetailsReturnsOk() {
		UUID cardId = UUID.randomUUID();
		CardDetailsDTO cardDetailsDTO = new CardDetailsDTO();
		given(this.cardDetailsService.getCardDetails(cardId)).willReturn(cardDetailsDTO);

		ResponseEntity<CardDetailsDTO> response = this.zetaApiController.getCardDetails(cardId);

		then(response.getStatusCode().value()).isEqualTo(200);
		then(response.getBody()).isSameAs(cardDetailsDTO);
	}

	@Test
	void testGetCardDetailsWithNullCardId() {
		given(this.cardDetailsService.getCardDetails(null)).willReturn(null);
		ResponseEntity<CardDetailsDTO> response = this.zetaApiController.getCardDetails(null);
		then(response.getStatusCode().value()).isEqualTo(200);
		then(response.getBody()).isNull();
	}

	@Test
	void testGetAuthExtractRecordReturnsAuthExtract() throws IOException {
		String receiptId = "RCT123456";
		AuthExtract authExtract = new AuthExtract();
		authExtract.setReceiptId(receiptId);
		given(this.cardDetailsService.getAuthExtractRecord(receiptId)).willReturn(authExtract);

		ResponseEntity<?> response = this.zetaApiController.getAuthExtractRecord(receiptId);

		then(response.getStatusCode().value()).isEqualTo(200);
		then(response.getBody()).isSameAs(authExtract);
	}

	@Test
	void testGenerateAuthExtractReturnsReceiptId() throws IOException {
		String cardId = UUID.randomUUID().toString();
		BigDecimal amount = new BigDecimal("125.50");
		String receiptId = "RCT123456";
		given(this.cardDetailsService.generateAuthExtract(cardId, amount)).willReturn(receiptId);

		ResponseEntity<?> response = this.zetaApiController.generateAuthExtract(cardId, amount);

		then(response.getStatusCode().value()).isEqualTo(200);
		then(response.getBody()).isEqualTo(receiptId);
	}

	@Test
	void testUploadGenerateAuthExtractReturnsTimestampBasedFileName() throws Exception {
		String receiptId = "receipt123";
		String cardId = UUID.randomUUID().toString();
		String fileName = "Card_Authorisation_Extract_10-Apr-2026_17_45_31.csv";
		given(this.cardDetailsService.uploadGenerateAuthExtract(receiptId, cardId)).willReturn(fileName);

		ResponseEntity<?> response = this.zetaApiController.uploadGenerateAuthExtract(receiptId, cardId);

		then(response.getStatusCode().value()).isEqualTo(200);
		then(response.getBody()).isEqualTo(fileName);
		then((String) response.getBody())
			.matches("Card_Authorisation_Extract_\\d{2}-[A-Za-z]{3}-\\d{4}_\\d{2}_\\d{2}_\\d{2}\\.csv");
	}

	@Test
	void testGenerateClearExtractReturnsReceiptId() throws IOException {
		String receiptId = "R12345";
		String settlementIndicator = "DEBIT";
		String cardId = UUID.randomUUID().toString();

		given(this.cardDetailsService.generateClearExtract(receiptId, settlementIndicator, cardId))
			.willReturn(receiptId);

		ResponseEntity<?> response = this.zetaApiController.generateClearExtract(receiptId, settlementIndicator,
				cardId);

		then(response.getStatusCode().value()).isEqualTo(200);
		then(response.getBody()).isEqualTo(receiptId);
	}

	@Test
	void testUploadGenerateAuthExtractReturnsOk() throws Exception {
		String receiptId = "receipt123";
		String cardId = UUID.randomUUID().toString();
		String fileName = "Card_Authorisation_Extract_02-Apr-2026_10_30_00.csv";
		given(this.cardDetailsService.uploadGenerateAuthExtract(receiptId, cardId)).willReturn(fileName);

		ResponseEntity<?> response = this.zetaApiController.uploadGenerateAuthExtract(receiptId, cardId);

		then(response.getStatusCode().value()).isEqualTo(200);
		then(response.getBody()).isEqualTo(fileName);
	}

	@Test
	void testGenerateAuthExtractFromDbReturnsResponse() throws Exception {
		LocalDate startDate = LocalDate.of(2026, 4, 1);
		BulkAuthFileResponseDto serviceResponse = new BulkAuthFileResponseDto();
		serviceResponse.setTotalAuthRecords(5);
		given(this.cardDetailsService.generateAuthFileFromDb(startDate.atStartOfDay())).willReturn(serviceResponse);

		ResponseEntity<BulkAuthFileResponseDto> response = this.zetaApiController.generateAuthExtractFromDb(startDate);

		then(response.getStatusCode().value()).isEqualTo(200);
		then(response.getBody()).isSameAs(serviceResponse);
		Mockito.verify(this.cardDetailsService).generateAuthFileFromDb(startDate.atStartOfDay());
	}

	@Test
	void testGenerateAuthEventsReturnsResponse() {
		LocalDate startDate = LocalDate.of(2026, 4, 10);
		int numberOfAuthEvents = 3;
		BulkAuthWebhookResponseDto serviceResponse = new BulkAuthWebhookResponseDto();
		serviceResponse.setProcessedCards(2);
		given(this.cardDetailsService.triggerAuthWebhooksFromDateTime(startDate.atStartOfDay(), numberOfAuthEvents,
				null))
			.willReturn(serviceResponse);

		ResponseEntity<BulkAuthWebhookResponseDto> response = this.zetaApiController.generateAuthEvents(startDate,
				numberOfAuthEvents, null);

		then(response.getStatusCode().value()).isEqualTo(200);
		then(response.getBody()).isSameAs(serviceResponse);
		Mockito.verify(this.cardDetailsService)
			.triggerAuthWebhooksFromDateTime(startDate.atStartOfDay(), numberOfAuthEvents, null);
	}

	@Test
	void testGenerateAuthEventsReturnsResponseWhenCardGuidsProvided() {
		LocalDate startDate = LocalDate.of(2026, 4, 10);
		int numberOfAuthEvents = 3;
		List<UUID> parsedCguids = List.of(UUID.randomUUID(), UUID.randomUUID());
		List<String> cguids = parsedCguids.stream().map(UUID::toString).toList();
		BulkAuthWebhookResponseDto serviceResponse = new BulkAuthWebhookResponseDto();
		serviceResponse.setProcessedCards(2);
		given(this.cardDetailsService.triggerAuthWebhooksFromDateTime(startDate.atStartOfDay(), numberOfAuthEvents,
				parsedCguids))
			.willReturn(serviceResponse);

		ResponseEntity<BulkAuthWebhookResponseDto> response = this.zetaApiController.generateAuthEvents(startDate,
				numberOfAuthEvents, cguids);

		then(response.getStatusCode().value()).isEqualTo(200);
		then(response.getBody()).isSameAs(serviceResponse);
		Mockito.verify(this.cardDetailsService)
			.triggerAuthWebhooksFromDateTime(startDate.atStartOfDay(), numberOfAuthEvents, parsedCguids);
	}

	@Test
	void testGenerateAuthEventsReturnsResponseWhenCardGuidsContainBlankValues() {
		LocalDate startDate = LocalDate.of(2026, 4, 10);
		int numberOfAuthEvents = 3;
		List<String> cguids = List.of(" ", "");
		BulkAuthWebhookResponseDto serviceResponse = new BulkAuthWebhookResponseDto();
		serviceResponse.setProcessedCards(2);
		given(this.cardDetailsService.triggerAuthWebhooksFromDateTime(startDate.atStartOfDay(), numberOfAuthEvents,
				null))
			.willReturn(serviceResponse);

		ResponseEntity<BulkAuthWebhookResponseDto> response = this.zetaApiController.generateAuthEvents(startDate,
				numberOfAuthEvents, cguids);

		then(response.getStatusCode().value()).isEqualTo(200);
		then(response.getBody()).isSameAs(serviceResponse);
		Mockito.verify(this.cardDetailsService)
			.triggerAuthWebhooksFromDateTime(startDate.atStartOfDay(), numberOfAuthEvents, null);
	}

	@Test
	void testGenerateAuthEventsThrowsBadRequestWhenCardGuidIsInvalid() {
		LocalDate startDate = LocalDate.of(2026, 4, 10);
		int numberOfAuthEvents = 3;

		org.assertj.core.api.Assertions
			.assertThatThrownBy(() -> this.zetaApiController.generateAuthEvents(startDate, numberOfAuthEvents,
					List.of("invalid-uuid")))
			.isInstanceOf(ResponseStatusException.class)
			.hasMessageContaining("400 BAD_REQUEST")
			.hasMessageContaining("Invalid cguid");
	}

	@Test
	void testGenerateClearExtractFromDbReturnsResponse() throws Exception {
		LocalDate startDate = LocalDate.of(2026, 4, 1);
		BulkClearFileResponseDto serviceResponse = new BulkClearFileResponseDto();
		serviceResponse.setTotalClearRecords(6);
		given(this.cardDetailsService.generateClearFileFromDb(startDate.atStartOfDay())).willReturn(serviceResponse);

		ResponseEntity<BulkClearFileResponseDto> response = this.zetaApiController
			.generateClearExtractFromDb(startDate);

		then(response.getStatusCode().value()).isEqualTo(200);
		then(response.getBody()).isSameAs(serviceResponse);
		Mockito.verify(this.cardDetailsService).generateClearFileFromDb(startDate.atStartOfDay());
	}

}
