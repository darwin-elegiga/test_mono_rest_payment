package com.uhg.optum.financials.payments.payer.dto.webhook;

import lombok.Getter;
import lombok.Setter;

import java.util.Arrays;
import java.util.HashSet;
import java.util.Set;

@Getter
@Setter
public class LoadingWebhookRequestDto<T> extends WebhookDataDto<T> {

	private static final Set<String> ALLOWED_TYPES = new HashSet<>(Arrays.asList("unload.failed", "unload.requested",
			"loading.completed", "loading.payment.failed", "loading.payment.settled", "loading.sent", "loading.denied",
			"loading.failed", "loading.payment.void.failed", "loading.payment.voided"));

	@Override
	public void setType(String type) {
		if (!type.isEmpty() && ALLOWED_TYPES.contains(type)) {
			super.setType(type);
		}
		else {
			throw new IllegalArgumentException("Invalid type value: " + type);
		}
	}

	@Override
	public String toString() {
		return "LoadingWebhookRequestDto " + super.toString();
	}

}
