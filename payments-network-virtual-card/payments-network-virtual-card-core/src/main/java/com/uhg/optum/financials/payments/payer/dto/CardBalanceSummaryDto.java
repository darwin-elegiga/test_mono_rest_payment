package com.uhg.optum.financials.payments.payer.dto;

import com.fasterxml.jackson.annotation.JsonFormat;
import lombok.Getter;
import lombok.Setter;

import java.math.BigDecimal;

@Setter
@Getter
public class CardBalanceSummaryDto {

	private String cguid;

	@JsonFormat(shape = JsonFormat.Shape.STRING)
	private BigDecimal availableBalance;

	@JsonFormat(shape = JsonFormat.Shape.STRING)
	private BigDecimal currentBalance;

	private String currency;

	private String timestamp;

}
