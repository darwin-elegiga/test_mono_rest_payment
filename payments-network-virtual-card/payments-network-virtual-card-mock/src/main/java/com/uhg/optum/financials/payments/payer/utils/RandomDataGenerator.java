package com.uhg.optum.financials.payments.payer.utils;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.security.SecureRandom;
import java.time.Instant;
import java.time.ZoneId;
import java.time.format.DateTimeFormatter;
import java.util.Locale;
import java.util.Random;
import java.util.UUID;

public final class RandomDataGenerator {

	private static final Random R = new SecureRandom();

	private static final DateTimeFormatter TS_FMT = DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mm:ssXXX")
		.withZone(ZoneId.systemDefault());

	private static final String[] NETWORKS = { "VISA", "MASTERCARD", "AMEX", "DISCOVER" };

	private static final String[] CURRENCIES = { "USD", "EUR", "GBP", "CAD" };

	private static final String[] CHANNELS = { "WEB", "MOBILE", "POS" };

	private static final String[] PAYMENT_CODES = { "001", "002", "003" };

	private RandomDataGenerator() {
	}

	public static String uuid() {
		return UUID.randomUUID().toString();
	}

	public static String uuidPrefix(String prefix) {
		return prefix + "-" + UUID.randomUUID().toString().split("-")[0];
	}

	public static String randomDigits(int len) {
		StringBuilder sb = new StringBuilder(len);
		for (int i = 0; i < len; i++) {
			sb.append(R.nextInt(10));
		}
		return sb.toString();
	}

	public static String randomAlphaNumeric(int len) {
		String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		StringBuilder sb = new StringBuilder(len);
		for (int i = 0; i < len; i++) {
			sb.append(chars.charAt(R.nextInt(chars.length())));
		}
		return sb.toString();
	}

	public static String randomFrom(String... items) {
		return items[R.nextInt(items.length)];
	}

	public static String randomNetwork() {
		return randomFrom(NETWORKS);
	}

	public static String randomCurrency() {
		return randomFrom(CURRENCIES);
	}

	public static String randomAmount() {
		BigDecimal v = BigDecimal.valueOf(1 + (R.nextDouble() * 9999)).setScale(2, RoundingMode.HALF_UP);
		return v.toPlainString();
	}

	public static String maskedCardNumber() {
		String last4 = randomDigits(4);
		return "************" + last4;
	}

	public static String timestampNow() {
		return TS_FMT.format(Instant.now());
	}

	public static String timestampPlusMinutes(int minutes) {
		return TS_FMT.format(Instant.now().plusSeconds(minutes * 60L));
	}

	public static String yesNo() {
		return R.nextBoolean() ? "Y" : "N";
	}

	public static String maybeEmpty(String value, double emptyProbability) {
		return (R.nextDouble() < emptyProbability) ? "" : value;
	}

	public static String randomChannelCode() {
		int idx = R.nextInt(CHANNELS.length);
		return String.valueOf(100 + idx);
	}

	public static String randomChannelName() {
		return randomFrom(CHANNELS);
	}

	public static String randomPaymentCode() {
		return randomFrom(PAYMENT_CODES);
	}

	public static String randomPaymentCodeName() {
		return "PAY_" + randomAlphaNumeric(3);
	}

	public static String randomMerchantName() {
		return "MERCH_" + randomAlphaNumeric(5);
	}

	public static String randomCity() {
		String[] cities = { "Chicago", "Houston", "New York", "Phoenix", "Seattle" };
		return randomFrom(cities);
	}

	public static String randomState() {
		String[] states = { "IL", "TX", "NY", "AZ", "WA" };
		return randomFrom(states);
	}

	public static String randomCountry() {
		String[] countries = { "US", "CA", "GB", "AU" };
		return randomFrom(countries);
	}

	public static String randomMerchantCategory() {
		return String.format(Locale.US, "%04d", R.nextInt(10000));
	}

}
