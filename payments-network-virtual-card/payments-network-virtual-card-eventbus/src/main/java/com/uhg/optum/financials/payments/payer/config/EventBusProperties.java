package com.uhg.optum.financials.payments.payer.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

import java.util.List;

@ConfigurationProperties(prefix = "eventbus")
public record EventBusProperties(List<String> topics) {

	public EventBusProperties {
		topics = (topics != null) ? List.copyOf(topics) : List.of();
	}

}
