package com.uhg.optum.financials.payments.payer.dto.authevent;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class Money {

	@Getter
	@Setter
	private String value;

	@Getter
	@Setter
	private String currency;

}
