package com.uhg.optum.financials.payments.payer.config;

import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.util.List;

@Configuration
@EnableConfigurationProperties({ RateLimitingProperties.class, ZetaApiProperties.class, StatesConfig.class })
public class RateLimitingCoreConfig {

	@Bean
	public RateLimiter bucketRateLimiter(RateLimitingProperties properties) {
		return new BucketRateLimiter(resolveMaxBuckets(properties));
	}

	private int resolveMaxBuckets(RateLimitingProperties properties) {
		if (properties.getMaxBuckets() > 0) {
			return properties.getMaxBuckets();
		}
		List<RateLimitingProperties.EndpointRule> rules = properties.getEndpoints();
		if (rules == null || rules.isEmpty()) {
			return 0;
		}
		// One key per distinct endpoint rule at minimum; include small headroom for
		// config changes.
		long enabledRules = rules.stream()
			.filter((rule) -> rule != null && Boolean.TRUE.equals(rule.getEnabled()))
			.count();
		if (enabledRules <= 0) {
			return 0;
		}
		return Math.toIntExact(Math.min(Integer.MAX_VALUE, enabledRules * 2));
	}

}
