package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.security.VirtualCardAccessAuthorizer;
import com.uhg.optum.financials.payments.payer.service.impl.VirtualCardBalanceService;
import com.uhg.optum.financials.shared.api.security.authorization.AuthorizeAccess;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.constraints.NotBlank;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.validation.annotation.Validated;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@Tag(name = "Card Balance Details")
@RequestMapping("v1/cards")
@Validated
public class CardBalanceController {

	@Autowired
	private VirtualCardBalanceService cardBalanceService;

	@Operation(summary = "Get Card Balances",
			description = "Endpoint to retrieve the balance details of a virtual card by its payment ID.")
	@GetMapping("/{paymentId}/balances")
	// @PreAuthorize("hasAuthority('org:networks:read')")
	@AuthorizeAccess(policyName = "org:networks:read", authorizer = VirtualCardAccessAuthorizer.class)
	public ResponseEntity<?> getCardBalances(@PathVariable("paymentId") @NotBlank String paymentId) throws Exception {
		return this.cardBalanceService.getCardBalances(paymentId);
	}

}
