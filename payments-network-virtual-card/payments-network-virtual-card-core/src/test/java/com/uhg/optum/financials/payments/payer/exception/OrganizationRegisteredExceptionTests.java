package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class OrganizationRegisteredExceptionTests {

	@Test
	void testOrganizationRegisteredException_message() {
		OrganizationRegisteredException exception = new OrganizationRegisteredException(
				"organization already registered");
		assertThat(exception).isNotNull();
		assertThat(exception.getMessage()).isEqualTo("organization already registered");
	}

	@Test
	void testOrganizationRegisteredException_isRuntimeException() {
		assertThatThrownBy(() -> {
			throw new OrganizationRegisteredException("fail");
		}).isInstanceOf(RuntimeException.class).hasMessage("fail");
	}

}
