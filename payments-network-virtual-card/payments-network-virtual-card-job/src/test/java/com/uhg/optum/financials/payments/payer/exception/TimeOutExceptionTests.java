package com.uhg.optum.financials.payments.payer.exception;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.assertj.core.api.AssertionsForClassTypes.assertThat;

@ExtendWith(MockitoExtension.class)
class TimeOutExceptionTests {

	@Test
	void testTimeOutException() {
		TimeOutException timeOutException = new TimeOutException("error");
		assertThat(timeOutException).isNotNull();
	}

}
