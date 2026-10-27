package com.uhg.optum.financials.payments.payer.constant;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;

class RuleTypeTests {

	@Test
	void ruleTypeEnum_shouldContainExpectedValues() {
		assertThat(this.getGlobal()).isEqualTo(RuleType.GLOBAL);
		assertThat(this.getGeneral()).isEqualTo(RuleType.GENERAL);
		assertThat(this.getCustom()).isEqualTo(RuleType.CUSTOM);
		assertThat(RuleType.values()).containsExactly(RuleType.GLOBAL, RuleType.GENERAL, RuleType.CUSTOM);
	}

	@Test
	void allowedValues_shouldMatchEnumNames() {
		assertThat(RuleType.ALLOWED_VALUES).isEqualTo("GLOBAL, GENERAL, CUSTOM");
	}

	private RuleType getGlobal() {
		return RuleType.GLOBAL;
	}

	private RuleType getGeneral() {
		return RuleType.GENERAL;
	}

	private RuleType getCustom() {
		return RuleType.CUSTOM;
	}

}
