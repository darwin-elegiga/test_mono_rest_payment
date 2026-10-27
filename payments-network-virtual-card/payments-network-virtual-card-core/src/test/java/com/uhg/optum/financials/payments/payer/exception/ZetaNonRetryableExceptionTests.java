package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class ZetaNonRetryableExceptionTests {

	@Test
	void testZetaNonRetryableException_messageAndStatusCode() {
		ZetaNonRetryableException exception = new ZetaNonRetryableException("non-retryable error", 403);
		assertThat(exception).isNotNull();
		assertThat(exception.getMessage()).isEqualTo("non-retryable error");
		assertThat(exception.getStatusCode()).isEqualTo(403);
	}

	@Test
	void testZetaNonRetryableException_isRuntimeException() {
		assertThatThrownBy(() -> {
			throw new ZetaNonRetryableException("fail", 500);
		}).isInstanceOf(RuntimeException.class).hasMessage("fail");
	}

}
