package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.ReprocessRequestDto;
import com.uhg.optum.financials.payments.payer.service.ReprocessPaymentService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.util.StringUtils;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.INFO;

@RestController
@Slf4j
@Tag(name = "Payment Re-Processing")
@RequestMapping("/v1/payment-reprocess")
public class PaymentReProcessingController {

	@Autowired
	ReprocessPaymentService reprocessPaymentService;

	@Autowired
	LogUtility logUtility;

	@Operation(summary = "Reprocess a Payment",
			description = "**NOTE:** Triggers the reprocessing of payment request(s) with PE status, "
					+ "identified by either a correlation ID (reprocesses every matching payment in that "
					+ "correlation group) or a single payment ID. Exactly one of the two must be provided.")
	@PreAuthorize("hasAuthority('org:networks:write')")
	@PostMapping
	public ResponseEntity<String> reprocessPayment(@RequestBody ReprocessRequestDto request) {
		boolean hasCorrelationId = StringUtils.hasText(request.getCorrelationId());
		boolean hasPaymentId = StringUtils.hasText(request.getPaymentId());

		if (hasCorrelationId == hasPaymentId) {
			return ResponseEntity.badRequest().body("Provide exactly one of correlationId or paymentId.");
		}

		String correlationId = hasCorrelationId ? request.getCorrelationId() : null;
		String paymentId = hasPaymentId ? request.getPaymentId() : null;
		String identifier = hasCorrelationId ? correlationId : paymentId;
		this.logUtility.logMessage("payment reprocess request received : " + identifier, INFO, log);

		int count = this.reprocessPaymentService.getReprocessPaymentRequestCount(correlationId, paymentId);
		if (count == 0) {
			return ResponseEntity.status(HttpStatus.NOT_FOUND)
				.body("No payment request found with PE status for the given "
						+ (hasCorrelationId ? "correlation ID." : "payment ID."));
		}

		boolean triggered = this.reprocessPaymentService.tryReprocessPaymentRequest(correlationId, paymentId);
		if (!triggered) {
			return ResponseEntity.status(HttpStatus.CONFLICT)
				.body("A reprocess is already in progress for the given "
						+ (hasCorrelationId ? "correlation ID." : "payment ID."));
		}
		return ResponseEntity.ok("Reprocess successfully :" + count);
	}

}
