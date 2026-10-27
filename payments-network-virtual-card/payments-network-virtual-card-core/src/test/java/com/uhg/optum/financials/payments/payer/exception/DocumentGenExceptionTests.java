package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class DocumentGenExceptionTests {

	@Test
	void testDocumentGenException_message() {
		DocumentGenException exception = new DocumentGenException("doc gen error");
		assertThat(exception).isNotNull();
		assertThat(exception.getMessage()).isEqualTo("doc gen error");
	}

	@Test
	void testDocumentGenException_isRuntimeException() {
		assertThatThrownBy(() -> {
			throw new DocumentGenException("fail");
		}).isInstanceOf(RuntimeException.class).hasMessage("fail");
	}

}
