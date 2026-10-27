package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.constant.AppConstants;

import com.uhg.optum.financials.payments.payer.dto.BulkLoadPaymentResponseDto;
import com.uhg.optum.financials.payments.payer.dto.BulkLoadPaymentRequestDto;
import com.uhg.optum.financials.payments.payer.service.BulkLoadPaymentService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.server.ResponseStatusException;

@RestController
@Slf4j
@Tag(name = "Payments", description = "Mock endpoints for payment operations")
@RequestMapping("v1/payment")
public class LoadPaymentMockController {

	private final BulkLoadPaymentService bulkLoadPaymentService;

	private final LogUtility logUtility;

	public LoadPaymentMockController(BulkLoadPaymentService bulkLoadPaymentService, LogUtility logUtility) {
		this.bulkLoadPaymentService = bulkLoadPaymentService;
		this.logUtility = logUtility;
	}

	@Operation(summary = "Bulk Load Payments",
			description = "**NOTE:** Triggers N individual payment requests by calling the Load Payment API endpoint once per payment. "
					+ "Each payment is assigned a unique auto-generated paymentId. "
					+ "A shared correlationId is generated for the entire bulk operation. This endpoint is used to test Rate Limiting and bulk operations.")
	@PostMapping("/bulk-load/{numberOfPayments}/{orgShortCode}/{accountShortCode}/{subAccountShortCode}/{paymentType}")
	public ResponseEntity<BulkLoadPaymentResponseDto> bulkLoadPayments(
			@Parameter(description = "Total number of payments to create", example = "5",
					schema = @io.swagger.v3.oas.annotations.media.Schema(type = "integer", format = "int32",
							minimum = "1", maximum = "500")) @PathVariable int numberOfPayments,

			@Parameter(description = "Organization short code", example = "UHG01",
					schema = @io.swagger.v3.oas.annotations.media.Schema(
							type = "string")) @PathVariable String orgShortCode,

			@Parameter(description = "Account short code", example = "FIN123",
					schema = @io.swagger.v3.oas.annotations.media.Schema(
							type = "string")) @PathVariable String accountShortCode,

			@Parameter(description = "Sub-account short code", example = "SUB456",
					schema = @io.swagger.v3.oas.annotations.media.Schema(
							type = "string")) @PathVariable String subAccountShortCode,

			@Parameter(description = "Payment type", example = "C2B",
					schema = @io.swagger.v3.oas.annotations.media.Schema(type = "string", allowableValues = { "C2B",
							"B2B" })) @PathVariable BulkLoadPaymentRequestDto.PaymentCategory paymentType) {

		if (numberOfPayments < 1 || numberOfPayments > 500) {
			throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "numberOfPayments must be between 1 and 600");
		}

		BulkLoadPaymentRequestDto bulkRequest = new BulkLoadPaymentRequestDto();
		bulkRequest.setNumberOfPayments(numberOfPayments);
		bulkRequest.setOrgShortCode(orgShortCode);
		bulkRequest.setAccountShortCode(accountShortCode);
		bulkRequest.setSubAccountShortCode(subAccountShortCode);
		bulkRequest.setPaymentType(paymentType);

		this.logUtility.logMessage(
				String.format(
						"[org=%s] [account=%s] [subAccount=%s] Bulk load request received for %d payment(s), type=%s",
						orgShortCode, accountShortCode, subAccountShortCode, numberOfPayments, paymentType),
				AppConstants.INFO, log);

		BulkLoadPaymentResponseDto response = this.bulkLoadPaymentService.bulkLoadPayments(bulkRequest);
		return ResponseEntity.ok(response);
	}

}
