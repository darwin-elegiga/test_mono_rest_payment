package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.CardDetailsDTO;
import com.uhg.optum.financials.payments.payer.dto.BulkAuthFileResponseDto;
import com.uhg.optum.financials.payments.payer.dto.BulkAuthWebhookResponseDto;
import com.uhg.optum.financials.payments.payer.dto.BulkClearFileResponseDto;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.service.MockZetaApiCardService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.server.ResponseStatusException;

import java.io.IOException;
import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

@RestController
@Slf4j
@RequestMapping("v1/card")
public class ZetaApiController {

	@Autowired
	private MockZetaApiCardService cardDetailsService;

	@PostMapping("/get-card-detail")
	public ResponseEntity<CardDetailsDTO> getCardDetails(@RequestParam UUID cardId) {
		CardDetailsDTO dto = this.cardDetailsService.getCardDetails(cardId);
		return ResponseEntity.ok(dto);
	}

	@PostMapping("/generate-auth-extract/{cardId}")
	public ResponseEntity<?> generateAuthExtract(@RequestParam String cardId, @RequestBody BigDecimal amount)
			throws IOException {
		String receiptId = this.cardDetailsService.generateAuthExtract(cardId, amount);
		return ResponseEntity.ok(receiptId);
	}

	@GetMapping("/get-auth-extract-record")
	public ResponseEntity<?> getAuthExtractRecord(@RequestParam String receiptId) throws IOException {
		AuthExtract authExtract = this.cardDetailsService.getAuthExtractRecord(receiptId);
		return ResponseEntity.ok(authExtract);
	}

	@PostMapping("/upload/generate-auth-extract/{cardId}")
	public ResponseEntity<?> uploadGenerateAuthExtract(@RequestParam String receiptId, @PathVariable String cardId)
			throws IOException {
		String fileName = this.cardDetailsService.uploadGenerateAuthExtract(receiptId, cardId);
		return ResponseEntity.ok(fileName);
	}

	@PostMapping("/generate-clear-extract")
	public ResponseEntity<?> generateClearExtract(@RequestParam String receiptId,
			@RequestParam String settlementIndicator, @RequestParam String cardId) throws IOException {
		String generatedReceiptId = this.cardDetailsService.generateClearExtract(receiptId, settlementIndicator,
				cardId);
		return ResponseEntity.ok(generatedReceiptId);
	}

	@PostMapping("/generate-auth-events")
	@Operation(summary = "Generate mock auth events",
			description = "Generates mock authorization webhook events for virtual cards created since startDate. "
					+ "When cguid is not provided (or only blank values are provided), all cards in the date range are used. "
					+ "When cguid is provided, only matching card GUIDs in that date range are processed.")
	public ResponseEntity<BulkAuthWebhookResponseDto> generateAuthEvents(
			@Parameter(description = "Start date (inclusive) used to find eligible virtual cards. Format: yyyy-MM-dd",
					example = "2026-04-16") @RequestParam("startDate") @DateTimeFormat(
							iso = DateTimeFormat.ISO.DATE) LocalDate startDate,
			@Parameter(description = "Number of auth events to generate per selected card. Must be greater than 0",
					example = "2") @RequestParam("numberOfAuthEvents") int numberOfAuthEvents,
			@Parameter(
					description = "Optional list of card GUID filters. Repeat query parameter to pass multiple values. "
							+ "Blank values are ignored.",
					example = "11111111-1111-1111-1111-111111111111") @RequestParam(value = "cguid",
							required = false) List<String> cguids) {
		List<UUID> parsedCguids = parseCguids(cguids);
		BulkAuthWebhookResponseDto response = this.cardDetailsService
			.triggerAuthWebhooksFromDateTime(startDate.atStartOfDay(), numberOfAuthEvents, parsedCguids);
		return ResponseEntity.ok(response);
	}

	@PostMapping("/upload/generate-auth-extract/by-date-range")
	@Operation(summary = "Generate auth extract file from DB",
			description = "Builds a Card Authorisation Extract CSV from auth records in the database "
					+ "from startDate (inclusive) to now, then returns file metadata such as file name, "
					+ "record count, and card count.")
	public ResponseEntity<BulkAuthFileResponseDto> generateAuthExtractFromDb(
			@Parameter(description = "Start date (inclusive) used to fetch auth records. Format: yyyy-MM-dd",
					example = "2026-04-16") @RequestParam("startDate") @DateTimeFormat(
							iso = DateTimeFormat.ISO.DATE) LocalDate startDate)
			throws IOException {
		BulkAuthFileResponseDto response = this.cardDetailsService.generateAuthFileFromDb(startDate.atStartOfDay());
		return ResponseEntity.ok(response);
	}

	@PostMapping("/upload/generate-clear-extract/by-date-range")
	public ResponseEntity<BulkClearFileResponseDto> generateClearExtractFromDb(
			@RequestParam("startDate") @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate startDate)
			throws IOException {
		BulkClearFileResponseDto response = this.cardDetailsService.generateClearFileFromDb(startDate.atStartOfDay());
		return ResponseEntity.ok(response);
	}

	private List<UUID> parseCguids(List<String> cguids) {
		if (cguids == null || cguids.isEmpty()) {
			return null;
		}

		List<UUID> parsed = new ArrayList<>();
		for (String cguid : cguids) {
			if (cguid == null || cguid.isBlank()) {
				continue;
			}
			try {
				parsed.add(UUID.fromString(cguid.trim()));
			}
			catch (IllegalArgumentException ex) {
				throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Invalid cguid: " + cguid);
			}
		}
		return parsed.isEmpty() ? null : parsed;
	}

}
