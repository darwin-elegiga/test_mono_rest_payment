package com.uhg.optum.financials.payments.payer.config;

import java.time.Duration;

/**
 * Strategy abstraction for request rate limiting.
 *
 * @author Payments Team
 */
public interface RateLimiter {

	BucketRateLimiter.Decision evaluate(String key, int limit, Duration window);

}
