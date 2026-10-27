package com.uhg.optum.financials.payments.payer.dto.webhook;

import java.util.HashSet;
import java.util.List;
import java.util.Set;

public class PaymentActivityWebhookRequestDto extends WebhookDataDto<PaymentActivityWebhookRequestPayload> {

	private static final Set<String> ALLOWED_TYPES = new HashSet<>(List.of("payment.activity"));

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
		return "PaymentActivityWebhookRequestDto " + super.toString();
	}

}
