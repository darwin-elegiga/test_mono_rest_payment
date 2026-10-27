package com.uhg.optum.financials.payments.payer.constant;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;

class ErrorCodesTests {

	@Test
	void allConstants_shouldHaveExpectedValues() {
		assertThat(ErrorCodes.ERROR_30).isEqualTo("Error 30: ");
		assertThat(ErrorCodes.ERROR_40).isEqualTo("Error 40: ");
		assertThat(ErrorCodes.ERROR_50).isEqualTo("Error 50: ");
		assertThat(ErrorCodes.P3).isEqualTo("P3");
		assertThat(ErrorCodes.P4).isEqualTo("P4");
		assertThat(ErrorCodes.P5).isEqualTo("P5");
	}

}
