package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.PaymentRequestTotalDto;
import com.uhg.optum.financials.payments.payer.model.BatchDetail;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

@RestController
@Slf4j
@Tag(name = "Batch Detail")
@RequestMapping("/v1/batch")
public class BatchDetailController {

	private final BatchDetailService batchDetailService;

	public BatchDetailController(BatchDetailService batchDetailService) {
		this.batchDetailService = batchDetailService;
	}

	@Operation(summary = "Get payments by correlation ID",
			description = "Retrieves every payment request associated with the supplied correlation ID, including each payment's processing status, payment details ID, and amount, along with the total number of matching payments.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/get/payment-by/{correlationId}")
	public PaymentRequestTotalDto getPaymentCountAndAmountByStatus(@PathVariable String correlationId) {
		return this.batchDetailService.getPaymentCountAndAmountByStatus(correlationId);
	}

	@Operation(summary = "Get payment details by correlation ID",
			description = "Retrieves every batch detail row associated with the supplied correlation ID.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/{correlationId}/payments")
	public List<BatchDetail> getBatchDetailsByCorrelationId(@PathVariable String correlationId) {
		return this.batchDetailService.getBatchDetailsByCorrelationId(correlationId);
	}

}
