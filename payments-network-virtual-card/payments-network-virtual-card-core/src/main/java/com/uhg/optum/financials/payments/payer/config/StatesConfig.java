package com.uhg.optum.financials.payments.payer.config;

import lombok.Getter;
import org.springframework.boot.context.properties.ConfigurationProperties;

import java.util.Map;

@Getter
@ConfigurationProperties(prefix = "us")
public class StatesConfig {

	private final Map<String, String> states;

	public StatesConfig(Map<String, String> states) {
		this.states = (states != null) ? Map.copyOf(states) : Map.of();
	}

}
