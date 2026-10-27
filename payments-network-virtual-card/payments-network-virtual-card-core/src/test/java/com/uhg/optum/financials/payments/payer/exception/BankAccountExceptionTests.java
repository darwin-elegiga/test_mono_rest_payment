package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class BankAccountExceptionTests {

	@Test
	void testBankAccountException_message() {
		BankAccountException exception = new BankAccountException("bank error");
		assertThat(exception).isNotNull();
		assertThat(exception.getMessage()).isEqualTo("bank error");
	}

	@Test
	void testBankAccountException_isRuntimeException() {
		assertThatThrownBy(() -> {
			throw new BankAccountException("fail");
		}).isInstanceOf(RuntimeException.class).hasMessage("fail");
	}

}
