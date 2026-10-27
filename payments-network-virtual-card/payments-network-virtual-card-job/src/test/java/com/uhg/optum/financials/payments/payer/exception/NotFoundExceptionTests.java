package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.assertj.core.api.AssertionsForClassTypes.assertThat;

@ExtendWith(MockitoExtension.class)
class NotFoundExceptionTests {

	@Test
	void testNotFoundException() {
		NotFoundException notFoundException = new NotFoundException("error");
		assertThat(notFoundException).isNotNull();
	}

}
