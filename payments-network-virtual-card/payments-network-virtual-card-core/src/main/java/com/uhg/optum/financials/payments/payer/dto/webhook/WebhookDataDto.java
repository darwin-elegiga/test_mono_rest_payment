package com.uhg.optum.financials.payments.payer.dto.webhook;

import lombok.Getter;
import lombok.Setter;

public abstract class WebhookDataDto<T> extends WebhookBaseDto {

	@Getter
	@Setter
	private T data;

}
