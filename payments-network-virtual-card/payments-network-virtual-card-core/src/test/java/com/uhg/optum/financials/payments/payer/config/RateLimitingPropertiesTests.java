package com.uhg.optum.financials.payments.payer.config;

import org.junit.jupiter.api.Test;

import java.time.Duration;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

class RateLimitingPropertiesTests {

	@Test
	void settersAndGetters_shouldBindExpectedEndpointRuleValues() {
		RateLimitingProperties properties = getRateLimitingProperties();

		assertThat(properties.getEndpoints()).hasSize(1);
		assertThat(properties.getEndpoints().getFirst().getEnabled()).isTrue();
		assertThat(properties.getEndpoints().getFirst().getPathPattern()).isEqualTo("/v1/**");
		assertThat(properties.getEndpoints().getFirst().getMethod()).isEqualTo("POST");
		assertThat(properties.getEndpoints().getFirst().getLimit()).isEqualTo(30);
		assertThat(properties.getEndpoints().getFirst().getWindow()).isEqualTo(Duration.ofHours(1));
	}

	@Test
	void endpointRule_shouldHaveEnabledValue() {
		RateLimitingProperties.EndpointRule endpointRule = new RateLimitingProperties.EndpointRule(true, "/v1/**",
				"POST", 30, Duration.ofHours(1));

		assertThat(endpointRule.getEnabled()).isTrue();
	}

	private static RateLimitingProperties getRateLimitingProperties() {
		RateLimitingProperties.EndpointRule endpointRule = new RateLimitingProperties.EndpointRule(true, "/v1/**",
				"POST", 30, Duration.ofHours(1));

		return new RateLimitingProperties(List.of(endpointRule), 100);
	}

}
