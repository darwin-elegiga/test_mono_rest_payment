package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import org.springframework.http.HttpStatusCode;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class ClientErrorExceptionTests {

	@Test
	void testClientErrorException_messageAndStatusCode() {
		HttpStatusCode status = HttpStatusCode.valueOf(400);
		ClientErrorException exception = new ClientErrorException("bad request", status);
		assertThat(exception).isNotNull();
		assertThat(exception.getMessage()).isEqualTo("bad request");
		assertThat(exception.getStatusCode()).isEqualTo(400);
	}

	@Test
	void testClientErrorException_isRuntimeException() {
		HttpStatusCode status = HttpStatusCode.valueOf(404);
		assertThatThrownBy(() -> {
			throw new ClientErrorException("not found", status);
		}).isInstanceOf(RuntimeException.class).hasMessage("not found");
	}

}
