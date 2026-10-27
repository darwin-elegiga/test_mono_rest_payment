package com.uhg.optum.financials.payments.payer.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import io.swagger.v3.oas.annotations.media.Schema;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class BulkLoadPaymentResponseDto {

	@Schema(description = "Overall status of the bulk operation", example = "SUCCESS")
	private String status;

	@Schema(description = "Shared correlation ID for all payments in this bulk request", example = "CORR12345")
	private String correlationId;

	@Schema(description = "Total number of payments triggered", example = "5")
	private int totalPaymentsTriggered;

	@Schema(description = "List of triggered payment entries")
	private List<PaymentEntry> payments;

	@Getter
	@Setter
	@NoArgsConstructor
	@AllArgsConstructor
	public static class PaymentEntry {

		@JsonProperty("paymentId")
		@Schema(description = "Unique payment ID", example = "PAY001")
		private String paymentId;

	}

}
