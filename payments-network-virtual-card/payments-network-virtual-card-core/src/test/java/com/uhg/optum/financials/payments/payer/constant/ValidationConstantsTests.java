package com.uhg.optum.financials.payments.payer.constant;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;

class ValidationConstantsTests {

	@Test
	void allConstants_shouldHaveExpectedValues() {
		assertThat(ValidationConstants.ACCOUNT_NUMBER_SIZE).isEqualTo(17);
		assertThat(ValidationConstants.MESSAGE).isEqualTo("message");
		assertThat(ValidationConstants.EVENT_HUB_VALIDATOR).isEqualTo("eventHubValidator");
		assertThat(ValidationConstants.ERROR).isEqualTo("error");
		assertThat(ValidationConstants.DEBUG).isEqualTo("debug");
		assertThat(ValidationConstants.INFO).isEqualTo("info");
		assertThat(ValidationConstants.WARN).isEqualTo("warn");
	}

}
