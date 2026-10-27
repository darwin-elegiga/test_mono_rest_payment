package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.AccountRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.AccountUpdatedDto;
import com.uhg.optum.financials.payments.payer.service.AccountPublisherService;
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
@RequestMapping("v1/accounts")
@Tag(name = "Accounts", description = "Mocking the Accounts events")
public class AccountController {

	@Autowired
	AccountPublisherService accountPublisherService;

	@Operation(summary = "Account Registration",
			description = "This mock endpoint is used to register an account in the system. "
					+ "It processes the account registration event and sends it to the event bus.")
	@PostMapping("/register")
	public ResponseEntity<String> produceAccountRegistered(@RequestBody AccountRegisteredEvent accountRegisteredEvent) {
		log.info("Sending account registered message to event bus");
		return ResponseEntity.ok(this.accountPublisherService.publishAccountRegisteredEvent(accountRegisteredEvent));
	}

	@Operation(summary = "Account Update",
			description = "This mock endpoint is used to update an account in the system. "
					+ "It processes the account update event and sends it to the event bus.")
	@PostMapping("/update")
	public ResponseEntity<String> produceAccountUpdated(@RequestBody AccountUpdatedDto accountRegisteredEvent) {
		log.info("Sending account updated message to event bus");
		return ResponseEntity.ok(this.accountPublisherService.publishAccountUpdatedEvent(accountRegisteredEvent));
	}

	@GetMapping("/{accountId}")
	public ResponseEntity<?> getAccount(@PathVariable String accountId) {
		log.info("Fetching account registration details for accountId: {}", sanitizeForLog(accountId));
		return ResponseEntity.ok(this.accountPublisherService.getAccountRegDetails(accountId));
	}

}
