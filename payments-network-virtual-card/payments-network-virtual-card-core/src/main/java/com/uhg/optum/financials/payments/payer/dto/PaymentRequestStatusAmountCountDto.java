package com.uhg.optum.financials.payments.payer.dto;

import io.swagger.v3.oas.annotations.media.Schema;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.Setter;

import java.math.BigDecimal;

@Getter
@Setter
@AllArgsConstructor
public class PaymentRequestStatusAmountCountDto {

	@Schema(description = "Status of the payment", example = "FT")
	String status;

	@Schema(description = "Number of payments with this status", example = "10")
	Long numberOfPayments;

	@Schema(description = "Total amount for this status", example = "5000.00")
	BigDecimal totalAmount;

}
