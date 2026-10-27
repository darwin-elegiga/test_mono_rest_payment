package com.uhg.optum.financials.payments.payer.dto.webhook;

import lombok.Getter;
import lombok.Setter;

public class WebhookErrorPayload {

	@Getter
	@Setter
	private String message;

	@Getter
	@Setter
	private String code;

}
