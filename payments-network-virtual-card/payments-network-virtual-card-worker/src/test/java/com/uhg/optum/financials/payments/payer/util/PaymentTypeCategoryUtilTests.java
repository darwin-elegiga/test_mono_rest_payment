package com.uhg.optum.financials.payments.payer.util;

import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;

class PaymentTypeCategoryUtilTests {

	@Test
	void toC2bOrB2b_returnsC2b_whenValueIsC2b() {
		assertThat(PaymentTypeCategoryUtil.toC2bOrB2b("C2B")).isEqualTo("C2B");
	}

	@Test
	void toC2bOrB2b_returnsC2b_whenValueIsMixedCaseC2b() {
		assertThat(PaymentTypeCategoryUtil.toC2bOrB2b(" c2b ")).isEqualTo("C2B");
	}

	@Test
	void toC2bOrB2b_returnsB2b_forNull() {
		assertThat(PaymentTypeCategoryUtil.toC2bOrB2b(null)).isEqualTo("B2B");
	}

	@Test
	void toC2bOrB2b_returnsB2b_forAnyOtherValue() {
		assertThat(PaymentTypeCategoryUtil.toC2bOrB2b("PAYMENT")).isEqualTo("B2B");
	}

}
