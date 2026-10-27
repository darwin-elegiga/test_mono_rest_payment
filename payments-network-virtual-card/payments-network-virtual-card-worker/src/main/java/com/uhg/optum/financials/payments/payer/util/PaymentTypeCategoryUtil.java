package com.uhg.optum.financials.payments.payer.util;

public final class PaymentTypeCategoryUtil {

	private static final String C2B = "C2B";

	private static final String B2B = "B2B";

	private PaymentTypeCategoryUtil() {
	}

	public static String toC2bOrB2b(String paymentDetailsType) {
		String value = paymentDetailsType;
		if (value != null) {
			value = value.trim();
		}
		return C2B.equalsIgnoreCase(value) ? C2B : B2B;
	}

}
