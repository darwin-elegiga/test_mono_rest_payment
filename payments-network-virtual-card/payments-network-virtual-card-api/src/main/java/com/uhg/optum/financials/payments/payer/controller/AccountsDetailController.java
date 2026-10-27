package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.account.AccountDto;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto;
import com.uhg.optum.financials.payments.payer.model.events.BankAccount;
import com.uhg.optum.financials.payments.payer.model.events.Organization;
import com.uhg.optum.financials.payments.payer.service.AccountsDetailService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.annotation.Lazy;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;

@RestController
@Slf4j
@Tag(name = "Accounts Detail")
@RequestMapping("/v1/accounts/get")
public class AccountsDetailController {

	private final AccountsDetailService accountsDetailService;

	private final LogUtility logUtility;

	public AccountsDetailController(AccountsDetailService accountsDetailService, LogUtility logUtility) {
		this.accountsDetailService = accountsDetailService;
		this.logUtility = logUtility;
	}

	@Operation(summary = "Get Bank Account Details",
			description = " **NOTE:** Retrieves detailed information about a specific bank account using its unique identifier.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/bank-account/{bankAccountId}")
	@Lazy
	public ResponseEntity<BankAccount> getBankAccountDetails(@PathVariable UUID bankAccountId) {
		this.logUtility.logMessage("Getting bank account details", INFO, log);
		return ResponseEntity.ok(this.accountsDetailService.getBankAccountDetails(bankAccountId));
	}

	@Operation(summary = "Get Account Details",
			description = " **NOTE:** Retrieves detailed information about a registered account using the provided organization short code and account short code.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/account/{orgShortCode}/{accountShortCode}")
	@Lazy
	public ResponseEntity<AccountDto> getAccount(@PathVariable String orgShortCode,
			@PathVariable String accountShortCode) {
		this.logUtility.logMessage("Getting payer Details", INFO, log);
		return ResponseEntity.ok(this.accountsDetailService.getAccount(accountShortCode, orgShortCode));
	}

	@Operation(summary = "Get Sub Account Details",
			description = " **NOTE:** Retrieves detailed information about a registered sub-account using the provided organization short code, account short code, and sub-account short code.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/sub-account/{orgShortCode}/{accountShortCode}/{subAccountShortCode}")
	@Lazy
	public ResponseEntity<SubAccountDto> getSubAccount(@PathVariable String orgShortCode,
			@PathVariable String accountShortCode, @PathVariable String subAccountShortCode) {
		this.logUtility.logMessage("Getting subAccount Details", INFO, log);
		return ResponseEntity
			.ok(this.accountsDetailService.getSubAccount(accountShortCode, subAccountShortCode, orgShortCode));
	}

	@Operation(summary = "Get Organization Details",
			description = " **NOTE:** Fetches detailed information about a registered organization using its short code identifier.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/organization/{orgShortCode}")
	@Lazy
	public ResponseEntity<Organization> getOrganization(@PathVariable String orgShortCode) {
		this.logUtility.logMessage("Getting organization  Details", INFO, log);
		return ResponseEntity.ok(this.accountsDetailService.getOrganizationRegisteredDetails(orgShortCode));
	}

}
