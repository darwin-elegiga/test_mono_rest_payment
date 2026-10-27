package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class InvalidCardDeclineAuthFileExceptionTests {

	@Test
	void testInvalidCardDeclineAuthFileException_message() {
		InvalidCardDeclineAuthFileException exception = new InvalidCardDeclineAuthFileException(
				"invalid card decline auth file error");
		assertThat(exception).isNotNull();
		assertThat(exception.getMessage()).isEqualTo("invalid card decline auth file error");
	}

	@Test
	void testInvalidCardDeclineAuthFileException_isRuntimeException() {
		assertThatThrownBy(() -> {
			throw new InvalidCardDeclineAuthFileException("fail");
		}).isInstanceOf(RuntimeException.class).hasMessage("fail");
	}

}
