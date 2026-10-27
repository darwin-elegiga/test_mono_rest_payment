package com.uhg.optum.financials.payments.payer.config;

import lombok.Getter;
import org.springframework.boot.context.properties.ConfigurationProperties;

import java.util.List;

@Getter
@ConfigurationProperties(prefix = "load-payment.card-callback")
public class LoadPaymentCardCallbackProperties {

	private final boolean enabled;

	private final List<Long> retryDelaysMinutes;

	public LoadPaymentCardCallbackProperties(boolean enabled, List<Long> retryDelaysMinutes) {
		this.enabled = enabled;
		this.retryDelaysMinutes = (retryDelaysMinutes != null) ? List.copyOf(retryDelaysMinutes) : List.of();
	}

}
