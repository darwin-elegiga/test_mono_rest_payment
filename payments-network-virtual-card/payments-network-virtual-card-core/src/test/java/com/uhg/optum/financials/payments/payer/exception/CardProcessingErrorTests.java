package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class CardProcessingErrorTests {

	@Test
	void testCardProcessingError_message() {
		CardProcessingError exception = new CardProcessingError("card error");
		assertThat(exception).isNotNull();
		assertThat(exception.getMessage()).isEqualTo("card error");
	}

	@Test
	void testCardProcessingError_messageAndCause() {
		Throwable cause = new IllegalArgumentException("cause");
		CardProcessingError exception = new CardProcessingError("card error", cause);
		assertThat(exception.getMessage()).isEqualTo("card error");
		assertThat(exception.getCause()).isEqualTo(cause);
	}

	@Test
	void testCardProcessingError_isRuntimeException() {
		assertThatThrownBy(() -> {
			throw new CardProcessingError("fail");
		}).isInstanceOf(RuntimeException.class).hasMessage("fail");
	}

}
