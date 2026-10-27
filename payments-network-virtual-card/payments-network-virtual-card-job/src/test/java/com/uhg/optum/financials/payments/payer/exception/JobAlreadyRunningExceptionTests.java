package com.uhg.optum.financials.payments.payer.exception;

import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
class JobAlreadyRunningExceptionTests {

	@InjectMocks
	JobAlreadyRunningException jobAlreadyRunningException;

	@Test
	void init() {
		String message = "";
		this.jobAlreadyRunningException = new JobAlreadyRunningException();
		this.jobAlreadyRunningException = new JobAlreadyRunningException(message);
		Assertions.assertThat(this.jobAlreadyRunningException).isNotNull();

	}

}
