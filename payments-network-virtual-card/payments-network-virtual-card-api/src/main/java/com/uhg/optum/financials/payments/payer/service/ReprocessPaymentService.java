package com.uhg.optum.financials.payments.payer.service;

import org.springframework.scheduling.annotation.Async;

public interface ReprocessPaymentService {

	/**
	 * Exactly one of {@code correlationId}/{@code paymentId} is expected to be non-null —
	 * the caller (controller) is responsible for that validation.
	 */
	int getReprocessPaymentRequestCount(String correlationId, String paymentId);

	/**
	 * Synchronous — returns {@code false} immediately if a reprocess is already in
	 * progress for this identifier (another trigger holds the lock), rather than racing
	 * it off the same stale state. For a correlationId, this acquires the lock and kicks
	 * off the bulk sweep in the background, returning {@code true} once it's underway.
	 * For a paymentId, the single dispatch completes synchronously before returning.
	 */
	boolean tryReprocessPaymentRequest(String correlationId, String paymentId);

	/**
	 * Internal — only called via the self-reference in the implementation, so the
	 * {@code @Async} proxy is actually invoked (calling it via {@code this.} from within
	 * the same class bypasses Spring's proxying and would run synchronously). Not meant
	 * to be called directly by other beans.
	 */
	@Async
	void reprocessByCorrelationIdAsync(String correlationId, String lockKey);

}
