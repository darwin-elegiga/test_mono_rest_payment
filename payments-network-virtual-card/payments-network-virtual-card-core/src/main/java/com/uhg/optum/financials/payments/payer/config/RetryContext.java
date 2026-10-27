package com.uhg.optum.financials.payments.payer.config;

import org.springframework.stereotype.Component;

@Component
public class RetryContext {

	private static final ThreadLocal<Integer> retryCount = ThreadLocal.withInitial(() -> 0);

	public void increment() {
		retryCount.set(retryCount.get() + 1);
	}

	public int get() {
		return retryCount.get();
	}

	public void reset() {
		retryCount.remove();
	}

}
