package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class UpdateEventsExceptionTests {

	@Test
	void testUpdateEventsException_message() {
		UpdateEventsException exception = new UpdateEventsException("update events error");
		assertThat(exception).isNotNull();
		assertThat(exception.getMessage()).isEqualTo("update events error");
	}

	@Test
	void testUpdateEventsException_isRuntimeException() {
		assertThatThrownBy(() -> {
			throw new UpdateEventsException("fail");
		}).isInstanceOf(RuntimeException.class).hasMessage("fail");
	}

}
