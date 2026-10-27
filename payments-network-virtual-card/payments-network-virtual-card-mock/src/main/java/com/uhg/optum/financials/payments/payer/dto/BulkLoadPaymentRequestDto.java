package com.uhg.optum.financials.payments.payer.dto;

import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class BulkLoadPaymentRequestDto {

	@NotNull(message = "numberOfPayments is required")
	@Min(value = 1, message = "numberOfPayments must be at least 1")
	@Schema(description = "Total number of payments to create", example = "5", type = "integer", format = "int32")
	private Integer numberOfPayments;

	@NotBlank(message = "orgShortCode is required")
	@Schema(description = "Organization short code", example = "UHG01", type = "string")
	private String orgShortCode;

	@NotBlank(message = "accountShortCode is required")
	@Schema(description = "Account short code", example = "FIN123", type = "string")
	private String accountShortCode;

	@NotBlank(message = "subAccountShortCode is required")
	@Schema(description = "Sub-account short code", example = "SUB456", type = "string")
	private String subAccountShortCode;

	@NotNull(message = "paymentType is required")
	@Schema(description = "Payment type", example = "C2B", allowableValues = { "C2B", "B2B" })
	private PaymentCategory paymentType;

	public enum PaymentCategory {

		C2B, B2B

	}

}
