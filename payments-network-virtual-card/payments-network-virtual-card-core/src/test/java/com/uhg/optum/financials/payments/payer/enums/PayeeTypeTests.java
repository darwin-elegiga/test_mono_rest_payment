package com.uhg.optum.financials.payments.payer.enums;

import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class PayeeTypeTests {

	@Test
	void fromInput_shouldResolveLegacyAndExpandedB2BValues() {
		assertThat(PayeeType.fromInput("B")).isEqualTo(PayeeType.B2B);
		assertThat(PayeeType.fromInput("b2b")).isEqualTo(PayeeType.B2B);
		assertThat(PayeeType.fromInput("BB")).isEqualTo(PayeeType.B2B);
	}

	@Test
	void fromInput_shouldResolveLegacyAndExpandedC2BValues() {
		assertThat(PayeeType.fromInput("C")).isEqualTo(PayeeType.C2B);
		assertThat(PayeeType.fromInput("c2b")).isEqualTo(PayeeType.C2B);
		assertThat(PayeeType.fromInput("CB")).isEqualTo(PayeeType.C2B);
	}

	@Test
	void fromInput_shouldThrowForUnsupportedValues() {
		assertThatThrownBy(() -> PayeeType.fromInput("XYZ")).isInstanceOf(IllegalArgumentException.class);
	}

}
