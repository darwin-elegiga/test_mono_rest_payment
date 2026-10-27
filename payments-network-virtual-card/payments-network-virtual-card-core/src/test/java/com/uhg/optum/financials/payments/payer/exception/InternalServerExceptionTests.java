package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.assertj.core.api.AssertionsForClassTypes.assertThat;

@ExtendWith(MockitoExtension.class)
public class InternalServerExceptionTests {

	@InjectMocks
	InternalServerException internalServerException;

	@Test
	void testInternalServerException() {
		this.internalServerException = new InternalServerException("Internal server error occurred");
		assertThat(this.internalServerException).isNotNull();
	}

}
