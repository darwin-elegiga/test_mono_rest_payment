package com.uhg.optum.financials.payments.payer.config;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;

class RetryContextTests {

	private RetryContext retryContext;

	@BeforeEach
	void setUp() {
		this.retryContext = new RetryContext();
		this.retryContext.reset(); // Ensure clean state before each test
	}

	@Test
	void increment_shouldIncreaseRetryCount() {
		int initial = this.retryContext.get();
		this.retryContext.increment();
		assertThat(this.retryContext.get()).isEqualTo(initial + 1);
	}

	@Test
	void get_shouldReturnCurrentRetryCount() {
		assertThat(this.retryContext.get()).isEqualTo(0);
		this.retryContext.increment();
		assertThat(this.retryContext.get()).isEqualTo(1);
	}

	@Test
	void reset_shouldClearRetryCount() {
		this.retryContext.increment();
		assertThat(this.retryContext.get()).isEqualTo(1);
		this.retryContext.reset();
		assertThat(this.retryContext.get()).isEqualTo(0);
	}

}
