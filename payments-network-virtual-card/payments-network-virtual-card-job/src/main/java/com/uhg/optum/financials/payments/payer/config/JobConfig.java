package com.uhg.optum.financials.payments.payer.config;

import org.springframework.batch.core.configuration.JobRegistry;
import org.springframework.batch.core.configuration.support.MapJobRegistry;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class JobConfig {

	@Bean
	public JobRegistry jobRegistry() {
		return new MapJobRegistry();
	}

}
