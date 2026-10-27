package com.uhg.optum.financials.payments.payer.dto.webhook;

import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

public class CardEventWebhookPayload {

	@Getter
	@Setter
	private String status;

	@Getter
	@Setter
	private UUID id;

	@Getter
	@Setter
	private String bin;

	@Getter
	@Setter
	private String last4;

	@Getter
	@Setter
	private AuthorizationWebhookPayload authorization;

	@Getter
	@Setter
	private Balance balance;

}
