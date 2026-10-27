package com.uhg.optum.financials.payments.payer.dto.webhook;

import lombok.Getter;
import lombok.Setter;

import java.math.BigDecimal;

public class Balance {

	@Getter
	@Setter
	private BigDecimal initial;

	@Getter
	@Setter
	private BigDecimal current;

	@Getter
	@Setter
	private BigDecimal available;

}
