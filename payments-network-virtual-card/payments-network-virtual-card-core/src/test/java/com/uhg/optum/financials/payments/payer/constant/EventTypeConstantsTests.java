package com.uhg.optum.financials.payments.payer.constant;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;

class EventTypeConstantsTests {

	@Test
	void allConstants_shouldHaveExpectedValues() {
		assertThat(EventTypeConstants.VIRTUAL_CARD_CREATE).isEqualTo("virtualcard.create");
		assertThat(EventTypeConstants.VIRTUAL_CARD_CANCEL).isEqualTo("virtualcard.cancel");
		assertThat(EventTypeConstants.VIRTUAL_CARD_RESET).isEqualTo("virtualcard.reset");
		assertThat(EventTypeConstants.AUTH_EVENT).isEqualTo("virtualcard.auth");
		assertThat(EventTypeConstants.AUTH_EVENT_APPROVED).isEqualTo("APPROVED");
		assertThat(EventTypeConstants.AUTH_EVENT_DECLINED).isEqualTo("DECLINED");
		assertThat(EventTypeConstants.AUTH_PAYMENT_EFFECTED).isEqualTo("PAYMENT_EFFECTED");
	}

}
