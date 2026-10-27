package com.uhg.optum.financials.payments.payer.config;

import org.junit.jupiter.api.Test;

import java.time.Duration;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class BucketRateLimiterTests {

	private static final int DEFAULT_MAX_BUCKETS = 256;

	private static final String GET_RULE_KEY = "GET|/v1/**|SERVICE";

	private static final String LOAD_PAYMENT_RULE_KEY = "POST|/v1/payment/load|SERVICE";

	private static final Duration ONE_MINUTE = Duration.ofMinutes(1);

	@Test
	void evaluate_shouldAllowRequestsWithinWindowLimit() {
		BucketRateLimiter limiter = new BucketRateLimiter(DEFAULT_MAX_BUCKETS);

		BucketRateLimiter.Decision firstDecision = evaluate(limiter, GET_RULE_KEY, 2, ONE_MINUTE);
		BucketRateLimiter.Decision secondDecision = evaluate(limiter, GET_RULE_KEY, 2, ONE_MINUTE);

		assertThat(firstDecision.allowed()).isTrue();
		assertThat(secondDecision.allowed()).isTrue();
		assertThat(secondDecision.requestCount()).isEqualTo(2);
	}

	@Test
	void evaluate_shouldRejectRequestAfterLimitIsExceeded() {
		BucketRateLimiter limiter = new BucketRateLimiter(DEFAULT_MAX_BUCKETS);

		evaluate(limiter, GET_RULE_KEY, 2, ONE_MINUTE);
		evaluate(limiter, GET_RULE_KEY, 2, ONE_MINUTE);
		BucketRateLimiter.Decision decision = evaluate(limiter, GET_RULE_KEY, 2, ONE_MINUTE);

		assertThat(decision.allowed()).isFalse();
		assertThat(decision.requestCount()).isEqualTo(3);
		assertThat(decision.retryAfterSeconds()).isGreaterThan(0);
	}

	@Test
	void evaluate_shouldResetCounterWhenWindowElapsed() throws InterruptedException {
		BucketRateLimiter limiter = new BucketRateLimiter(DEFAULT_MAX_BUCKETS);

		evaluate(limiter, GET_RULE_KEY, 2, Duration.ofMillis(10));
		Thread.sleep(25);
		BucketRateLimiter.Decision afterWindowElapsed = evaluate(limiter, GET_RULE_KEY, 2, Duration.ofMillis(10));

		assertThat(afterWindowElapsed.allowed()).isTrue();
		assertThat(afterWindowElapsed.requestCount()).isEqualTo(1);
	}

	@Test
	void evaluate_shouldCreateSeparateBucketsWhenLimitConfigurationChanges() {
		BucketRateLimiter limiter = new BucketRateLimiter(DEFAULT_MAX_BUCKETS);

		BucketRateLimiter.Decision firstDecision = evaluate(limiter, GET_RULE_KEY, 2, ONE_MINUTE);
		BucketRateLimiter.Decision secondDecision = evaluate(limiter, GET_RULE_KEY, 3, ONE_MINUTE);
		BucketRateLimiter.Decision thirdDecision = evaluate(limiter, GET_RULE_KEY, 3, ONE_MINUTE);

		assertThat(firstDecision.allowed()).isTrue();
		assertThat(secondDecision.allowed()).isTrue();
		assertThat(secondDecision.requestCount()).isEqualTo(1);
		assertThat(thirdDecision.allowed()).isTrue();
		assertThat(thirdDecision.requestCount()).isEqualTo(2);
	}

	@Test
	void evaluate_shouldApplyLimitsPerLimiterInstance_whenSimulatingTwoPods() {
		BucketRateLimiter podALimiter = new BucketRateLimiter(DEFAULT_MAX_BUCKETS);
		BucketRateLimiter podBLimiter = new BucketRateLimiter(DEFAULT_MAX_BUCKETS);

		BucketRateLimiter.Decision podAFirst = evaluate(podALimiter, LOAD_PAYMENT_RULE_KEY, 2, ONE_MINUTE);
		BucketRateLimiter.Decision podASecond = evaluate(podALimiter, LOAD_PAYMENT_RULE_KEY, 2, ONE_MINUTE);
		BucketRateLimiter.Decision podAThird = evaluate(podALimiter, LOAD_PAYMENT_RULE_KEY, 2, ONE_MINUTE);

		BucketRateLimiter.Decision podBFirst = evaluate(podBLimiter, LOAD_PAYMENT_RULE_KEY, 2, ONE_MINUTE);
		BucketRateLimiter.Decision podBSecond = evaluate(podBLimiter, LOAD_PAYMENT_RULE_KEY, 2, ONE_MINUTE);
		BucketRateLimiter.Decision podBThird = evaluate(podBLimiter, LOAD_PAYMENT_RULE_KEY, 2, ONE_MINUTE);

		assertThat(podAFirst.allowed()).isTrue();
		assertThat(podASecond.allowed()).isTrue();
		assertThat(podAThird.allowed()).isFalse();

		assertThat(podBFirst.allowed()).isTrue();
		assertThat(podBSecond.allowed()).isTrue();
		assertThat(podBThird.allowed()).isFalse();

		assertThat(podAThird.requestCount()).isEqualTo(3);
		assertThat(podBThird.requestCount()).isEqualTo(3);
	}

	@Test
	void constructor_shouldAcceptCustomMaxBuckets() {
		BucketRateLimiter limiter = new BucketRateLimiter(512);
		assertThat(limiter).isNotNull();
	}

	@Test
	void constructor_shouldAllowZeroForDynamicMaxBuckets() {
		BucketRateLimiter limiter = new BucketRateLimiter(0);
		assertThat(limiter).isNotNull();
	}

	@Test
	void constructor_shouldThrowWhenMaxBucketsIsNegative() {
		assertThatThrownBy(() -> new BucketRateLimiter(-1)).isInstanceOf(IllegalArgumentException.class)
			.hasMessage("maxBuckets must be >= 0, got: -1");
	}

	private BucketRateLimiter.Decision evaluate(BucketRateLimiter limiter, String key, int limit, Duration window) {
		return limiter.evaluate(key, limit, window);
	}

}
