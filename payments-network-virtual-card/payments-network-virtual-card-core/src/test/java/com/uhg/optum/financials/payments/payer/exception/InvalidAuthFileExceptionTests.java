package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class InvalidAuthFileExceptionTests {

	@Test
	void testInvalidAuthFileException_message() {
		InvalidAuthFileException exception = new InvalidAuthFileException("invalid auth file error");
		assertThat(exception).isNotNull();
		assertThat(exception.getMessage()).isEqualTo("invalid auth file error");
	}

	@Test
	void testInvalidAuthFileException_isRuntimeException() {
		assertThatThrownBy(() -> {
			throw new InvalidAuthFileException("fail");
		}).isInstanceOf(RuntimeException.class).hasMessage("fail");
	}

}
