package com.uhg.optum.financials.payments.payer.util;

import java.math.BigDecimal;
import org.junit.jupiter.api.Test;
import static org.assertj.core.api.AssertionsForClassTypes.assertThat;
import static org.assertj.core.api.AssertionsForClassTypes.assertThatThrownBy;

class AmountConverterTests {

	@Test
	void toCents_nullAmount_throwsException() {
		assertThatThrownBy(() -> AmountConverter.toCents(null)).isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("Amount cannot be null");
	}

	@Test
	void toCents_fractionalCents_throwsException() {
		BigDecimal amount = BigDecimal.valueOf(10.005); // 1000.5 cents
		assertThatThrownBy(() -> AmountConverter.toCents(amount)).isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("Amount has fractional cents");
	}

	@Test
	void toCents_validAmount_returnsCents() {
		BigDecimal amount = BigDecimal.valueOf(12.34);
		Long cents = AmountConverter.toCents(amount);
		assertThat(cents).isEqualTo(1234L);
	}

	@Test
	void toCents_zeroAmount_returnsZero() {
		BigDecimal amount = BigDecimal.ZERO;
		Long cents = AmountConverter.toCents(amount);
		assertThat(cents).isEqualTo(0L);
	}

	@Test
	void toDollars_nullAmount_returnsZero() {
		BigDecimal dollars = AmountConverter.toDollars(null);
		assertThat(dollars).isEqualTo(BigDecimal.ZERO);
	}

	@Test
	void toDollars_validAmount_returnsDollars() {
		Long cents = 1234L;
		BigDecimal dollars = AmountConverter.toDollars(cents);
		assertThat(dollars).isEqualTo(BigDecimal.valueOf(12.34));
	}

	@Test
	void toDollars_zeroAmount_returnsZero() {
		Long cents = 0L;
		BigDecimal dollars = AmountConverter.toDollars(cents);
		assertThat(dollars).isEqualTo(BigDecimal.ZERO);
	}

}
