package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.assertj.core.api.AssertionsForClassTypes.assertThat;

@ExtendWith(MockitoExtension.class)
public class RegisterEventsExceptionTests {

	@InjectMocks

	RegisterEventsException registerEventsException;

	@Test
	void testRegisterEventsException() {
		this.registerEventsException = new RegisterEventsException("error");
		assertThat(this.registerEventsException).isNotNull();
	}

}
