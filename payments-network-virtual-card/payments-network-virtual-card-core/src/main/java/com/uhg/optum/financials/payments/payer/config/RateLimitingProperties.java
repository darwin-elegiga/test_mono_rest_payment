package com.uhg.optum.financials.payments.payer.config;

import lombok.Getter;
import org.springframework.boot.context.properties.ConfigurationProperties;

import java.time.Duration;
import java.util.List;

@Getter
@ConfigurationProperties(prefix = "rate-limiting")
public class RateLimitingProperties {

	private final List<EndpointRule> endpoints;

	/**
	 * Maximum number of cached rate-limit buckets.
	 * <p>
	 * Use a positive value to enforce a hard cap. Use {@code 0} to enable dynamic sizing
	 * based on configured endpoint rules.
	 */
	private final int maxBuckets;

	public RateLimitingProperties(List<EndpointRule> endpoints, int maxBuckets) {
		this.endpoints = (endpoints != null) ? List.copyOf(endpoints) : List.of();
		this.maxBuckets = maxBuckets;
	}

	@Getter
	public static class EndpointRule {

		private final Boolean enabled;

		private final String pathPattern;

		private final String method;

		private final Integer limit;

		private final Duration window;

		public EndpointRule(Boolean enabled, String pathPattern, String method, Integer limit, Duration window) {
			this.enabled = (enabled != null) ? enabled : true;
			this.pathPattern = pathPattern;
			this.method = method;
			this.limit = limit;
			this.window = window;
		}

	}

}
