package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class InvalidClearFileExceptionTests {

	@Test
	void testInvalidClearFileException_message() {
		InvalidClearFileException exception = new InvalidClearFileException("invalid clear file error");
		assertThat(exception).isNotNull();
		assertThat(exception.getMessage()).isEqualTo("invalid clear file error");
	}

	@Test
	void testInvalidClearFileException_isRuntimeException() {
		assertThatThrownBy(() -> {
			throw new InvalidClearFileException("fail");
		}).isInstanceOf(RuntimeException.class).hasMessage("fail");
	}

}
