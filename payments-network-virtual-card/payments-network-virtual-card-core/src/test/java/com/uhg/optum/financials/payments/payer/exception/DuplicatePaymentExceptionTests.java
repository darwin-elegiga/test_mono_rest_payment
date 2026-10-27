package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class DuplicatePaymentExceptionTests {

	@Test
	void testDuplicatePaymentException_message() {
		DuplicatePaymentException exception = new DuplicatePaymentException("duplicate payment error");
		assertThat(exception).isNotNull();
		assertThat(exception.getMessage()).isEqualTo("duplicate payment error");
	}

	@Test
	void testDuplicatePaymentException_isRuntimeException() {
		assertThatThrownBy(() -> {
			throw new DuplicatePaymentException("fail");
		}).isInstanceOf(RuntimeException.class).hasMessage("fail");
	}

}
