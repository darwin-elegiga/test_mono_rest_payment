package com.uhg.optum.financials.payments.payer.dto;

import com.fasterxml.jackson.annotation.JsonIgnore;
import io.swagger.v3.oas.annotations.media.Schema;
import lombok.Getter;
import lombok.Setter;

import java.math.BigDecimal;
import java.util.List;

@Getter
@Setter
public class PaymentRequestTotalDto {

	@Schema(description = "Total number of payments", example = "25")
	String totalPaymentCount;

	@Schema(description = "Total amount of all payments", example = "12500.75")
	@JsonIgnore
	String totalAmount;

	@JsonIgnore
	List<PaymentRequestStatusAmountCountDto> paymentRequestStatusAmountCountDto;

	List<PaymentRequest> paymentRequest;

	public record PaymentRequest(String status, String paymentId, BigDecimal amount) {

	}

}
