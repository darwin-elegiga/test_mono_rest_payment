package com.uhg.optum.financials.payments.payer.util;

import java.math.BigDecimal;

public final class AmountConverter {

	private AmountConverter() {
		// private constructor
	}

	/**
	 * Converts a BigDecimal amount in dollars to cents. Uses longValueExact() to ensure
	 * precision.
	 * @param amountInDollars the amount in dollars
	 * @return the amount in cents as Long
	 * @throws IllegalArgumentException if the amount has fractional cents
	 */
	public static Long toCents(BigDecimal amountInDollars) {
		if (amountInDollars == null) {
			throw new IllegalArgumentException("Amount cannot be null");
		}

		try {
			return amountInDollars.multiply(BigDecimal.valueOf(100)).longValueExact();
		}
		catch (ArithmeticException ex) {
			throw new IllegalArgumentException("Amount has fractional cents: " + amountInDollars, ex);
		}
	}

	/**
	 * Converts cents to dollars as BigDecimal.
	 * @param amountInCents the amount in cents
	 * @return the amount in dollars
	 */
	public static BigDecimal toDollars(Long amountInCents) {
		if (amountInCents == null) {
			return BigDecimal.ZERO;
		}

		return BigDecimal.valueOf(amountInCents).divide(BigDecimal.valueOf(100));
	}

}
