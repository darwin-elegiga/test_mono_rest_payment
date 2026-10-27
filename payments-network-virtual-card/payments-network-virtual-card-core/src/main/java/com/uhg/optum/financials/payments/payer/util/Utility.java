package com.uhg.optum.financials.payments.payer.util;

import org.springframework.util.ObjectUtils;

import java.math.BigDecimal;
import java.util.regex.Pattern;

public final class Utility {

	private static final Pattern LOG_CONTROL_CHARS = Pattern.compile("[\\r\\n\\t\\f\\u0000-\\u001F\\u007F]");

	private static final Pattern LOG_SAFE_ID_CHARS = Pattern.compile("[^A-Za-z0-9._-]");

	private Utility() {
	}

	public static boolean isNonEmpty(String str) {
		return !ObjectUtils.isEmpty(str);
	}

	public static String getString(Object obj, String defaultValue) {
		return ObjectUtils.isEmpty(obj) ? defaultValue : obj.toString();
	}

	public static BigDecimal getBigDecimal(Object obj, BigDecimal defaultValue) {
		return ObjectUtils.isEmpty(obj) ? defaultValue : (BigDecimal) obj;
	}

	public static String sanitizeForLog(String value) {
		if (value == null) {
			return null;
		}
		StringBuilder sanitized = new StringBuilder(value.length());
		for (int i = 0; i < value.length(); i++) {
			char ch = value.charAt(i);
			if (ch == '\r' || ch == '\n' || ch == '\t' || ch == '\f' || ch < 0x20 || ch == 0x7F) {
				sanitized.append('_');
			}
			else {
				sanitized.append(ch);
			}
		}
		return sanitized.toString();
	}

	public static String sanitizeIdForLog(String value) {
		if (value == null) {
			return null;
		}
		String controlCharSanitized = sanitizeForLog(value);
		return LOG_SAFE_ID_CHARS.matcher(controlCharSanitized).replaceAll("_");
	}

}
