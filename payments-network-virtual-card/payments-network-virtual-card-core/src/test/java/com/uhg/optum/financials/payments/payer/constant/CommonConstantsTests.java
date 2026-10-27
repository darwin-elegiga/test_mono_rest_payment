package com.uhg.optum.financials.payments.payer.constant;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;

class CommonConstantsTests {

	@Test
	void allConstants_shouldHaveExpectedValues() {
		assertThat(CommonConstants.ACCOUNT_NUMBER_SIZE).isEqualTo(17);
		assertThat(CommonConstants.MESSAGE).isEqualTo("message");
		assertThat(CommonConstants.EVENT_HUB_VALIDATOR).isEqualTo("eventHubValidator");
		assertThat(CommonConstants.ERROR).isEqualTo("error");
		assertThat(CommonConstants.DEBUG).isEqualTo("debug");
		assertThat(CommonConstants.INFO).isEqualTo("info");
		assertThat(CommonConstants.WARN).isEqualTo("warn");
		assertThat(CommonConstants.SUCCESS).isEqualTo("success");
		assertThat(CommonConstants.AUTH_ERROR).isEqualTo("Error occurred while processing AUTH with file path: ");
		assertThat(CommonConstants.HTTP_202).isEqualTo(202);
		assertThat(CommonConstants.HTTP_400).isEqualTo(400);
		assertThat(CommonConstants.HTTP_503).isEqualTo(503);
		assertThat(CommonConstants.HTTP_500).isEqualTo(500);
	}

}
