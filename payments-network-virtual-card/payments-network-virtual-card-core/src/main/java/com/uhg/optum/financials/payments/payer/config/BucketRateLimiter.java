package com.uhg.optum.financials.payments.payer.config;

import com.github.benmanes.caffeine.cache.Caffeine;
import com.github.benmanes.caffeine.cache.Expiry;
import com.github.benmanes.caffeine.cache.LoadingCache;
import io.github.bucket4j.Bandwidth;
import io.github.bucket4j.Bucket;
import io.github.bucket4j.ConsumptionProbe;

import java.time.Duration;
import java.util.concurrent.atomic.AtomicInteger;

public class BucketRateLimiter implements RateLimiter {

	private final LoadingCache<CacheKey, BucketCounter> counters;

	/**
	 * Constructor with configurable max buckets sourced from application.yml.
	 * (rate-limiting.max-buckets)
	 * @param maxBuckets maximum number of concurrent rate limit buckets to cache. A value
	 * of {@code 0} enables dynamic/unbounded growth (bounded only by expiry).
	 */
	public BucketRateLimiter(int maxBuckets) {
		if (maxBuckets < 0) {
			throw new IllegalArgumentException("maxBuckets must be >= 0, got: " + maxBuckets);
		}
		Caffeine<CacheKey, BucketCounter> builder = Caffeine.newBuilder().expireAfter(new WindowExpiry());
		if (maxBuckets > 0) {
			builder.maximumSize(maxBuckets);
		}
		this.counters = builder.build(this::newBucketCounter);
	}

	@Override
	public Decision evaluate(String key, int limit, Duration window) {
		CacheKey cacheKey = new CacheKey(key, limit, window);
		BucketCounter counter = this.counters.get(cacheKey);
		ConsumptionProbe probe = counter.bucket().tryConsumeAndReturnRemaining(1);
		int requestCount = counter.requestCount().incrementAndGet();
		return new Decision(probe.isConsumed(), requestCount, computeRetryAfterSeconds(probe));
	}

	private Bucket newBucket(int limit, Duration window) {
		Bandwidth bandwidth = Bandwidth.builder().capacity(limit).refillIntervally(limit, window).build();
		return Bucket.builder().addLimit(bandwidth).build();
	}

	private BucketCounter newBucketCounter(CacheKey cacheKey) {
		return new BucketCounter(newBucket(cacheKey.limit(), cacheKey.window()), new AtomicInteger());
	}

	private long computeRetryAfterSeconds(ConsumptionProbe probe) {
		if (probe.isConsumed()) {
			return 0;
		}
		return Math.max(1, (probe.getNanosToWaitForRefill() + 999_999_999L) / 1_000_000_000L);
	}

	private record CacheKey(String key, int limit, Duration window) {
	}

	private record BucketCounter(Bucket bucket, AtomicInteger requestCount) {
	}

	private static final class WindowExpiry implements Expiry<CacheKey, BucketCounter> {

		// Sliding expiry: extend TTL on read/update to keep hot keys in memory and
		// reduce churn during burst traffic. Tradeoff: stale entries remain longer
		// under intermittent access, increasing memory usage.

		@Override
		public long expireAfterCreate(CacheKey key, BucketCounter value, long currentTime) {
			return key.window().toNanos();
		}

		@Override
		public long expireAfterUpdate(CacheKey key, BucketCounter value, long currentTime, long currentDuration) {
			return key.window().toNanos();
		}

		@Override
		public long expireAfterRead(CacheKey key, BucketCounter value, long currentTime, long currentDuration) {
			return key.window().toNanos();
		}

	}

	public record Decision(boolean allowed, int requestCount, long retryAfterSeconds) {
	}

}
