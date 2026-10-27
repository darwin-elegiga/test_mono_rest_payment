package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;

import static org.assertj.core.api.AssertionsForClassTypes.assertThat;

public class UnauthorizedExceptionTests {

	@InjectMocks
	UnauthorizedException unauthorizedException;

	@Test
	void testUnauthorizedException() {
		this.unauthorizedException = new UnauthorizedException("Unauthorized access");
		assertThat(this.unauthorizedException).isNotNull();
	}

}
