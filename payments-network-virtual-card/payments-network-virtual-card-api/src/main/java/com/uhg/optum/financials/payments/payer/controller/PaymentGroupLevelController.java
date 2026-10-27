package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.constant.PaymentGroupingLevelType;
import com.uhg.optum.financials.payments.payer.model.PaymentGroupingLevel;
import com.uhg.optum.financials.payments.payer.service.PaymentGroupingLevelService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.enums.ParameterIn;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.DEBUG;

@RestController
@Slf4j
@Tag(name = "Org Level Grouping")
@RequestMapping("/v1")
public class PaymentGroupLevelController {

	private final LogUtility logUtility;

	private final PaymentGroupingLevelService paymentGroupingLevelService;

	public PaymentGroupLevelController(LogUtility logUtility, PaymentGroupingLevelService paymentGroupingLevelService) {
		this.logUtility = logUtility;
		this.paymentGroupingLevelService = paymentGroupingLevelService;
	}

	@Operation(summary = "Save organization-level payment grouping configuration",
			description = """
					 **NOTE:**
					This should be used to save the payment grouping level configuration for a specific organization.
					It accepts the organization short code and the desired grouping level type, and persists this configuration in the database.
					""")
	@PreAuthorize("hasAuthority('org:networks:write')")
	@PostMapping("/save-org-group-level/{orgShortCode}/{paymentGroupingLevelType}/{eobRequired}")
	public PaymentGroupingLevel savePaymentGroupLevel(
			@Parameter(name = "orgShortCode", in = ParameterIn.PATH,
					description = "Short code representing the organization",
					example = "OXFD5") @PathVariable String orgShortCode,

			@Parameter(name = "paymentGroupingLevelType", in = ParameterIn.PATH,
					description = "Type of payment grouping level to be saved",
					example = "ORG") @PathVariable PaymentGroupingLevelType paymentGroupingLevelType) {
		this.logUtility.logMessage("Loading paymentGroupingLevel from OptumPay Service", DEBUG, log);
		return this.paymentGroupingLevelService.savePaymentGroupingLevel(orgShortCode, paymentGroupingLevelType);
	}

	@Operation(summary = "Get Payment Grouping Level by Organization",
			description = """
					 **NOTE:**
					This should be used retrieves the payment grouping level configuration for a given organization, identified by its short code.
					""")
	@GetMapping("/get-org-group-level/{orgShortCode}")
	@PreAuthorize("hasAuthority('org:networks:read')")
	public PaymentGroupingLevel getPaymentGroupLevel(@PathVariable String orgShortCode) {
		this.logUtility.logMessage("Getting paymentGroupingLevel from OptumPay Service", DEBUG, log);
		return this.paymentGroupingLevelService.getPaymentGroupLevel(orgShortCode);
	}

}
