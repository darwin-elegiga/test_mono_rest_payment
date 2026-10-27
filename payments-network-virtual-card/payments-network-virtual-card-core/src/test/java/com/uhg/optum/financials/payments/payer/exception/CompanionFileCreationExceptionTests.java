package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class CompanionFileCreationExceptionTests {

	@Test
	void testCompanionFileCreationException_message() {
		CompanionFileCreationException exception = new CompanionFileCreationException("companion file error");
		assertThat(exception).isNotNull();
		assertThat(exception.getMessage()).isEqualTo("companion file error");
	}

	@Test
	void testCompanionFileCreationException_isRuntimeException() {
		assertThatThrownBy(() -> {
			throw new CompanionFileCreationException("fail");
		}).isInstanceOf(RuntimeException.class).hasMessage("fail");
	}

}
