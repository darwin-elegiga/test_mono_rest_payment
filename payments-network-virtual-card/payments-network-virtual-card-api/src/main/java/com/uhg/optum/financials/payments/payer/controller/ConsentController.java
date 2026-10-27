package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.ConsentSaveResponseDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStatesSaveRequestDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStatesResponseDto;
import com.uhg.optum.financials.payments.payer.service.ConsentService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping(path = "/v1/consent-required-states", produces = "application/json")
@RequiredArgsConstructor
@Tag(name = "Consent Details",
		description = "APIs for managing consent details, including retrieval of active US states and their consent requirement status.")
public class ConsentController {

	private final ConsentService consentService;

	@Operation(summary = "Get all US states",
			description = "Returns all US states and their consent requirement status.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping
	public ResponseEntity<ConsentStatesResponseDto> getAllStatesConsent() {
		return ResponseEntity.ok(this.consentService.getAllStatesConsent());
	}

	@Operation(summary = "Bulk save consent required states",
			description = "Accepts the final UI state list and synchronizes consent-required values in one request.")
	@PreAuthorize("hasAuthority('org:networks:write')")
	@PutMapping(consumes = "application/json")
	public ResponseEntity<ConsentSaveResponseDto> saveStatesConsent(
			@Valid @RequestBody ConsentStatesSaveRequestDto request) {
		return ResponseEntity.ok(this.consentService.saveStatesConsent(request));
	}

}
