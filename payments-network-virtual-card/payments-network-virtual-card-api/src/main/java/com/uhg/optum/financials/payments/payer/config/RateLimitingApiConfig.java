package com.uhg.optum.financials.payments.payer.config;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class RateLimitingApiConfig {

	@Bean
	public RateLimitingFilter rateLimitingFilter(RateLimiter rateLimiter, RateLimitingProperties properties,
			ObjectMapper objectMapper) {
		return new RateLimitingFilter(rateLimiter, properties, objectMapper);
	}

}
