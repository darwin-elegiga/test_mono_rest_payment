package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.ResponseDto;

import com.uhg.optum.financials.payments.payer.dto.bankaccount.BankAccountEventDto;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.BaseEncryptedBankAccountDto;
import com.uhg.optum.financials.payments.payer.service.BankAccountService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import jakarta.validation.constraints.NotNull;
import jakarta.xml.bind.ValidationException;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.PathVariable;

import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.Constants.SEND_MESSAGE_EVENTBUS_LOG;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;

@RestController
@Slf4j
@RequestMapping("/v1")
public class BankAccountController {

	@Autowired
	BankAccountService eventBusService;

	@Autowired
	private LogUtility logUtility;

	@Operation(summary = "Produce bank account event to EventBus",
			description = " **NOTE:** Publishes a bank account creation event to the EventBus for processing. This endpoint accepts bank account details and returns the routing number upon successful dispatch.")

	@PostMapping("/accounts/produce/bank-account/details")
	public ResponseEntity<ResponseDto<String>> sendMessage(@RequestBody BankAccountEventDto bankAccountEventMessage)
			throws ValidationException {
		this.logUtility.logMessage(SEND_MESSAGE_EVENTBUS_LOG, INFO, log);
		return ResponseEntity.ok(new ResponseDto<>(this.eventBusService.publishEvent(bankAccountEventMessage), true,
				bankAccountEventMessage.getBankAccountCreated().getRoutingNumber()));

	}

	@GetMapping("/bank-accounts/{id}/encrypted")
	public ResponseEntity<BaseEncryptedBankAccountDto> getEncryptedBankAccountById(
			@RequestHeader("x-optum-tenant-id") String tenantId, @PathVariable("id") @NotNull UUID id,
			@RequestHeader("x-origin-public-key-base64") String publicKeyBase64) {

		return ResponseEntity.ok(this.eventBusService.getEncryptedBankAccountById(id, publicKeyBase64, tenantId));
	}

}
