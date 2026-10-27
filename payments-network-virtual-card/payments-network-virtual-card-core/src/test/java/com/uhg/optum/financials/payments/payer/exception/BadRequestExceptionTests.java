package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.assertj.core.api.AssertionsForClassTypes.assertThat;

@ExtendWith(MockitoExtension.class)
class BadRequestExceptionTests {

	@InjectMocks
	BadRequestException badRequestException;

	@Test
	void testBadRequestException() {
		this.badRequestException = new BadRequestException("error");
		assertThat(this.badRequestException).isNotNull();
	}

}
