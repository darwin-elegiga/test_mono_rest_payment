package com.uhg.optum.financials.payments.payer.config;

import lombok.Getter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.boot.context.properties.bind.ConstructorBinding;

import java.util.List;

@ConfigurationProperties("com.optum.job")
@Getter
public class JobNameProviderConfig {

	private final List<String> name;

	@ConstructorBinding
	public JobNameProviderConfig(List<String> name) {
		this.name = (name != null) ? List.copyOf(name) : List.of();
	}

}
