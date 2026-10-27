package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.SubAccountRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.SubAccountUpdatedDto;
import com.uhg.optum.financials.payments.payer.service.SubAccountPublisherService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import static com.uhg.optum.financials.payments.payer.util.Utility.sanitizeForLog;

@RestController
@Slf4j
@RequestMapping("v1/sub-accounts")
@Tag(name = "Sub-Accounts", description = "Mocking the Sub-Accounts events")
public class SubAccountController {

	@Autowired
	SubAccountPublisherService subAccountPublisherService;

	@Operation(summary = "Sub-Account Registration",
			description = "This mock endpoint is used to register a sub-account in the system. "
					+ "It processes the sub-account registration event and sends it to the event bus.")
	@PostMapping("/register")
	public ResponseEntity<String> produceSubAccountRegistered(
			@RequestBody SubAccountRegisteredEvent subAccountRegisteredEvent) {
		log.info("Sending message to event bus");
		return ResponseEntity
			.ok(this.subAccountPublisherService.publishSubAccountRegisteredEvent(subAccountRegisteredEvent));
	}

	@Operation(summary = "Sub-Account Update",
			description = "This mock endpoint is used to update a sub-account in the system. "
					+ "It processes the sub-account update event and sends it to the event bus.")
	@PostMapping("/update")
	public ResponseEntity<String> produceSubAccountUpdated(@RequestBody SubAccountUpdatedDto subAccountUpdatedDto) {
		log.info("Sending sub account updated event to event bus");
		return ResponseEntity.ok(this.subAccountPublisherService.publishSubAccountUpdatedEvent(subAccountUpdatedDto));
	}

	@GetMapping("/{subAccountId}")
	public ResponseEntity<?> getSubAccount(@PathVariable String subAccountId) {
		String sanitizedSubAccountId = sanitizeForLog(subAccountId);
		log.info("Fetching sub-account registration details for subAccountId: {}", sanitizedSubAccountId);
		return ResponseEntity.ok(this.subAccountPublisherService.getSubAccountRegDetails(subAccountId));
	}

}
