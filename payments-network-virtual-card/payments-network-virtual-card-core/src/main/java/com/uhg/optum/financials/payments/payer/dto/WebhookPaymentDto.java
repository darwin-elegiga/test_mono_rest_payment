package com.uhg.optum.financials.payments.payer.dto;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.math.BigDecimal;
import java.util.Map;

@Setter
@Getter
@NoArgsConstructor
@AllArgsConstructor
public class WebhookPaymentDto {

	private String paymentDetailId;

	private String networkPaymentId;

	private BigDecimal paymentAmount;

	private String orgShortCode;

	private Map<String, String> metadata;

}
