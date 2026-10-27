package com.uhg.optum.financials.payments.payer.dto.webhook;

import lombok.Getter;
import lombok.Setter;

public class MerchantPayload {

	@Getter
	@Setter
	private String terminalId;

	@Getter
	@Setter
	private String categoryCode;

	@Getter
	@Setter
	private String name;

	@Getter
	@Setter
	private String city;

	@Getter
	@Setter
	private String state;

}
