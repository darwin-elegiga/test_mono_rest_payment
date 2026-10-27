package com.uhg.optum.financials.payments.payer.config;

import java.io.IOException;
import java.time.Duration;
import java.util.List;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.ServletRequest;
import jakarta.servlet.ServletResponse;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.mock.web.MockHttpServletRequest;
import org.springframework.mock.web.MockHttpServletResponse;

import static org.assertj.core.api.Assertions.assertThat;

class RateLimitingFilterTests {

	private static final int DEFAULT_MAX_BUCKETS = 256;

	private RateLimitingFilter filter;

	private CountingFilterChain countingFilterChain;

	@BeforeEach
	void setUp() {
		RateLimitingProperties.EndpointRule endpointRule = new RateLimitingProperties.EndpointRule(true, "/v1/**",
				"GET", 2, Duration.ofMinutes(1));
		RateLimitingProperties properties = new RateLimitingProperties(List.of(endpointRule), 0);

		this.filter = new RateLimitingFilter(new BucketRateLimiter(DEFAULT_MAX_BUCKETS), properties);
		this.countingFilterChain = new CountingFilterChain();
	}

	@Test
	void doFilter_shouldAllowRequestsWithinConfiguredLimit() throws ServletException, IOException {
		this.filter.doFilter(createRequest("GET", "/v1/cards/123"), new MockHttpServletResponse(),
				this.countingFilterChain);
		this.filter.doFilter(createRequest("GET", "/v1/cards/123"), new MockHttpServletResponse(),
				this.countingFilterChain);

		assertThat(this.countingFilterChain.getInvocationCount()).isEqualTo(2);
	}

	@Test
	void doFilter_shouldReturn429WhenConfiguredLimitIsExceeded() throws ServletException, IOException {
		MockHttpServletResponse throttledResponse = new MockHttpServletResponse();

		this.filter.doFilter(createRequest("GET", "/v1/cards/123"), new MockHttpServletResponse(),
				this.countingFilterChain);
		this.filter.doFilter(createRequest("GET", "/v1/cards/123"), new MockHttpServletResponse(),
				this.countingFilterChain);
		this.filter.doFilter(createRequest("GET", "/v1/cards/123"), throttledResponse, this.countingFilterChain);

		assertThat(this.countingFilterChain.getInvocationCount()).isEqualTo(2);
		assertThat(throttledResponse.getStatus()).isEqualTo(429);
		assertThat(throttledResponse.getHeader("Retry-After")).isNotBlank();
		assertThat(throttledResponse.getContentAsString()).contains("Rate limit exceeded");
	}

	@Test
	void doFilter_shouldBypassExcludedPath() throws ServletException, IOException {
		MockHttpServletResponse response = new MockHttpServletResponse();
		this.filter.doFilter(createRequest("GET", "/health-check"), response, this.countingFilterChain);

		assertThat(this.countingFilterChain.getInvocationCount()).isEqualTo(1);
		assertThat(response.getStatus()).isEqualTo(200);
	}

	@Test
	void doFilter_shouldBypassWhenNoEndpointRuleMatches() throws ServletException, IOException {
		MockHttpServletResponse response = new MockHttpServletResponse();
		this.filter.doFilter(createRequest("POST", "/cards/webhooks"), response, this.countingFilterChain);

		assertThat(this.countingFilterChain.getInvocationCount()).isEqualTo(1);
		assertThat(response.getStatus()).isEqualTo(200);
	}

	@Test
	void doFilter_shouldBypassWhenHttpMethodDoesNotMatchConfiguredRule() throws ServletException, IOException {
		MockHttpServletResponse response = new MockHttpServletResponse();
		this.filter.doFilter(createRequest("POST", "/v1/cards/123"), response, this.countingFilterChain);

		assertThat(this.countingFilterChain.getInvocationCount()).isEqualTo(1);
		assertThat(response.getStatus()).isEqualTo(200);
	}

	@Test
	void doFilter_shouldUseEndpointSpecificLimitWhenConfigured() throws ServletException, IOException {
		RateLimitingFilter filter = new RateLimitingFilter(new BucketRateLimiter(DEFAULT_MAX_BUCKETS),
				createPropertiesWithEndpointOverride());
		CountingFilterChain chain = new CountingFilterChain();
		MockHttpServletResponse throttledResponse = new MockHttpServletResponse();

		filter.doFilter(createRequest("POST", "/v1/cards/update"), new MockHttpServletResponse(), chain);
		filter.doFilter(createRequest("POST", "/v1/cards/update"), new MockHttpServletResponse(), chain);
		filter.doFilter(createRequest("POST", "/v1/cards/update"), new MockHttpServletResponse(), chain);
		filter.doFilter(createRequest("POST", "/v1/cards/update"), new MockHttpServletResponse(), chain);
		filter.doFilter(createRequest("POST", "/v1/cards/update"), new MockHttpServletResponse(), chain);
		filter.doFilter(createRequest("POST", "/v1/cards/update"), throttledResponse, chain);

		assertThat(chain.getInvocationCount()).isEqualTo(5);
		assertThat(throttledResponse.getStatus()).isEqualTo(429);
	}

	@Test
	void doFilter_shouldFallbackToDefaultLimitWhenEndpointDoesNotOverride() throws ServletException, IOException {
		RateLimitingFilter filter = new RateLimitingFilter(new BucketRateLimiter(DEFAULT_MAX_BUCKETS),
				createPropertiesWithMixedOverrides());
		CountingFilterChain chain = new CountingFilterChain();
		MockHttpServletResponse throttledResponse = new MockHttpServletResponse();

		filter.doFilter(createRequest("GET", "/v1/cards/123"), new MockHttpServletResponse(), chain);
		filter.doFilter(createRequest("GET", "/v1/cards/123"), new MockHttpServletResponse(), chain);
		filter.doFilter(createRequest("GET", "/v1/cards/123"), throttledResponse, chain);

		assertThat(chain.getInvocationCount()).isEqualTo(2);
		assertThat(throttledResponse.getStatus()).isEqualTo(429);
	}

	@Test
	void doFilter_shouldUseDifferentBucketsForDifferentRequestPathsMatchingSameEndpointRule()
			throws ServletException, IOException {
		MockHttpServletResponse throttledResponse = new MockHttpServletResponse();

		this.filter.doFilter(createRequest("GET", "/v1/cards/123"), new MockHttpServletResponse(),
				this.countingFilterChain);
		this.filter.doFilter(createRequest("GET", "/v1/cards/456"), new MockHttpServletResponse(),
				this.countingFilterChain);
		this.filter.doFilter(createRequest("GET", "/v1/cards/789"), throttledResponse, this.countingFilterChain);

		assertThat(this.countingFilterChain.getInvocationCount()).isEqualTo(3);
		assertThat(throttledResponse.getStatus()).isNotEqualTo(429);
	}

	@Test
	void doFilter_shouldPreferMostSpecificMatchingRuleWhenMultipleRulesMatch() throws ServletException, IOException {
		RateLimitingProperties.EndpointRule broadRule = new RateLimitingProperties.EndpointRule(true, "/v1/**", "GET",
				1, Duration.ofMinutes(1));

		RateLimitingProperties.EndpointRule specificRule = new RateLimitingProperties.EndpointRule(true, "/v1/cards/**",
				"GET", 2, Duration.ofMinutes(1));

		RateLimitingProperties properties = new RateLimitingProperties(List.of(broadRule, specificRule), 0);

		RateLimitingFilter filter = new RateLimitingFilter(new BucketRateLimiter(DEFAULT_MAX_BUCKETS), properties);
		CountingFilterChain chain = new CountingFilterChain();
		MockHttpServletResponse throttledResponse = new MockHttpServletResponse();

		filter.doFilter(createRequest("GET", "/v1/cards/123"), new MockHttpServletResponse(), chain);
		filter.doFilter(createRequest("GET", "/v1/cards/123"), new MockHttpServletResponse(), chain);
		filter.doFilter(createRequest("GET", "/v1/cards/123"), throttledResponse, chain);

		assertThat(chain.getInvocationCount()).isEqualTo(2);
		assertThat(throttledResponse.getStatus()).isEqualTo(429);
	}

	private MockHttpServletRequest createRequest(String method, String path) {
		MockHttpServletRequest request = new MockHttpServletRequest();
		request.setMethod(method);
		request.setServletPath(path);
		request.setRequestURI(path);
		return request;
	}

	private RateLimitingProperties createPropertiesWithEndpointOverride() {
		RateLimitingProperties.EndpointRule endpointRule = new RateLimitingProperties.EndpointRule(true,
				"/v1/cards/update", "POST", 5, Duration.ofMinutes(1));
		return new RateLimitingProperties(List.of(endpointRule), 0);
	}

	private RateLimitingProperties createPropertiesWithMixedOverrides() {
		RateLimitingProperties.EndpointRule getRule = new RateLimitingProperties.EndpointRule(true, "/v1/cards/**",
				"GET", 2, Duration.ofMinutes(1));

		RateLimitingProperties.EndpointRule postRule = new RateLimitingProperties.EndpointRule(true, "/v1/cards/update",
				"POST", 10, Duration.ofMinutes(2));

		return new RateLimitingProperties(List.of(getRule, postRule), 0);
	}

	private static final class CountingFilterChain implements FilterChain {

		private int invocationCount;

		@Override
		public void doFilter(ServletRequest request, ServletResponse response) {
			this.invocationCount++;
		}

		private int getInvocationCount() {
			return this.invocationCount;
		}

	}

}
