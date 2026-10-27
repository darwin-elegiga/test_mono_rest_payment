package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.payments.payer.constant.ApiConstants;
import com.fasterxml.jackson.databind.ObjectMapper;
import jakarta.annotation.Nonnull;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.lang3.StringUtils;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.util.AntPathMatcher;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.time.Duration;
import java.nio.charset.StandardCharsets;
import java.util.List;
import java.util.Optional;

/**
 * Applies rate limits to configured endpoint rules only.
 * <p>
 * Matching is based on the configured HTTP method and Ant-style path pattern, while
 * counters remain service-scoped so all matching requests share the same bucket
 * regardless of caller or user identity.
 *
 * @author Payments Team
 */
@Slf4j
public class RateLimitingFilter extends OncePerRequestFilter {

	private static final String KEY_DELIMITER = "|";

	private final RateLimiter rateLimiter;

	private final RateLimitingProperties properties;

	private final ObjectMapper objectMapper;

	private final AntPathMatcher pathMatcher = new AntPathMatcher();

	public RateLimitingFilter(RateLimiter rateLimiter, RateLimitingProperties properties) {
		this(rateLimiter, properties, new ObjectMapper());
	}

	public RateLimitingFilter(RateLimiter rateLimiter, RateLimitingProperties properties, ObjectMapper objectMapper) {
		this.rateLimiter = rateLimiter;
		this.properties = properties;
		this.objectMapper = objectMapper;
	}

	@Override
	protected boolean shouldNotFilter(@Nonnull HttpServletRequest request) {
		String requestPath = getRequestPath(request);
		return findMatchingRule(request.getMethod(), requestPath).isEmpty();
	}

	@Override
	protected void doFilterInternal(@Nonnull HttpServletRequest request, @Nonnull HttpServletResponse response,
			@Nonnull FilterChain filterChain) throws ServletException, IOException {
		String requestPath = getRequestPath(request);
		Optional<RateLimitingProperties.EndpointRule> ruleOpt = findMatchingRule(request.getMethod(), requestPath);
		if (ruleOpt.isEmpty()) {
			filterChain.doFilter(request, response);
			return;
		}

		RateLimitingProperties.EndpointRule rule = ruleOpt.get();
		Integer limit = rule.getLimit();
		Duration window = rule.getWindow();
		if (limit == null || limit <= 0 || window == null || window.isNegative() || window.isZero()) {
			filterChain.doFilter(request, response);
			return;
		}

		String key = buildCounterKey(rule, request.getMethod(), requestPath);
		BucketRateLimiter.Decision decision = this.rateLimiter.evaluate(key, limit, window);
		if (!decision.allowed()) {
			response.setStatus(HttpStatus.TOO_MANY_REQUESTS.value());
			response.setHeader(HttpHeaders.RETRY_AFTER, String.valueOf(decision.retryAfterSeconds()));
			response.setContentType(MediaType.APPLICATION_JSON_VALUE);
			response.setCharacterEncoding(StandardCharsets.UTF_8.name());
			RateLimitErrorResponse errorResponse = new RateLimitErrorResponse(HttpStatus.TOO_MANY_REQUESTS.value(),
					HttpStatus.TOO_MANY_REQUESTS.getReasonPhrase(), "Rate limit exceeded");
			response.getWriter().write(this.objectMapper.writeValueAsString(errorResponse));
			String matchedRuleMethod = StringUtils.defaultIfBlank(rule.getMethod(), request.getMethod()).toUpperCase();
			String matchedRulePathPattern = rule.getPathPattern();
			log.warn(
					"Rate limit exceeded for matchedMethod={}, matchedPathPattern={}, requestsInWindow={}, limit={}, window={}, scope={}, retryAfterSeconds={}",
					sanitize(matchedRuleMethod), sanitize(matchedRulePathPattern), decision.requestCount(), limit,
					window, ApiConstants.SERVICE_SCOPE_KEY, decision.retryAfterSeconds());
			return;
		}

		filterChain.doFilter(request, response);
	}

	private String getRequestPath(HttpServletRequest request) {
		String servletPath = request.getServletPath();
		if (StringUtils.isNotBlank(servletPath)) {
			return servletPath;
		}
		return request.getRequestURI();
	}

	private Optional<RateLimitingProperties.EndpointRule> findMatchingRule(String method, String requestPath) {
		List<RateLimitingProperties.EndpointRule> rules = this.properties.getEndpoints();
		if (rules == null || rules.isEmpty()) {
			return Optional.empty();
		}
		RateLimitingProperties.EndpointRule bestRule = null;
		var patternComparator = this.pathMatcher.getPatternComparator(requestPath);
		for (RateLimitingProperties.EndpointRule rule : rules) {
			if (rule == null || !Boolean.TRUE.equals(rule.getEnabled()) || StringUtils.isBlank(rule.getPathPattern())) {
				continue;
			}
			if (StringUtils.isNotBlank(rule.getMethod()) && !rule.getMethod().equalsIgnoreCase(method)) {
				continue;
			}
			if (this.pathMatcher.match(rule.getPathPattern(), requestPath)) {
				if (bestRule == null
						|| patternComparator.compare(rule.getPathPattern(), bestRule.getPathPattern()) < 0) {
					bestRule = rule;
				}
			}
		}
		return Optional.ofNullable(bestRule);
	}

	private String buildCounterKey(RateLimitingProperties.EndpointRule rule, String requestMethod, String requestPath) {
		String method = StringUtils.defaultIfBlank(rule.getMethod(), requestMethod).toUpperCase();
		return method + KEY_DELIMITER + rule.getPathPattern() + KEY_DELIMITER + requestPath + KEY_DELIMITER
				+ ApiConstants.SERVICE_SCOPE_KEY;
	}

	/**
	 * Sanitizes a user-provided value for safe log output by stripping newline and
	 * carriage-return characters that could enable log injection attacks.
	 */
	private String sanitize(String value) {
		if (value == null) {
			return "";
		}
		return value.replace('\r', '_').replace('\n', '_');
	}

	private record RateLimitErrorResponse(int status, String error, String message) {
	}

}
