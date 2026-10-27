package com.uhg.optum.financials.payments.payer.constant;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;

class PaymentGroupingLevelTypeTests {

	@Test
	void paymentGroupingLevelTypeEnum_shouldContainExpectedValues() {
		assertThat(this.getOrg()).isEqualTo(PaymentGroupingLevelType.ORG);
		assertThat(this.getAccount()).isEqualTo(PaymentGroupingLevelType.ACCOUNT);
		assertThat(this.getSubAccount()).isEqualTo(PaymentGroupingLevelType.SUB_ACCOUNT);
		assertThat(PaymentGroupingLevelType.values()).containsExactly(PaymentGroupingLevelType.ORG,
				PaymentGroupingLevelType.ACCOUNT, PaymentGroupingLevelType.SUB_ACCOUNT);
	}

	private PaymentGroupingLevelType getOrg() {
		return PaymentGroupingLevelType.ORG;
	}

	private PaymentGroupingLevelType getAccount() {
		return PaymentGroupingLevelType.ACCOUNT;
	}

	private PaymentGroupingLevelType getSubAccount() {
		return PaymentGroupingLevelType.SUB_ACCOUNT;
	}

}
