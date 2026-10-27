package com.uhg.optum.financials.payments.utilites;

import org.assertj.core.api.Assertions;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.math.BigDecimal;
import java.security.SecureRandom;
import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.time.DayOfWeek;
import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Calendar;
import java.util.Date;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.Random;
import java.util.Set;
import java.util.TimeZone;
import java.util.stream.Collectors;
import java.util.stream.Stream;

@Service
public class Helper {

	private static final Logger log = LoggerFactory.getLogger(Helper.class);

	public static String currentDate = getCurrentDate("yyyy-MM-dd");

	public static final String REGEX_PIPE = "\\|";

	private String holidayDetails = "2026-01-01|2026-01-19|2026-02-16|2026-05-25|2026-06-19|2026-07-04|2026-09-07|2026-10-12|2026-11-11|2026-11-26|2026-12-25";

	public static String generateRandomAlphabetsString(int length) {
		Random rd = new Random();
		String aphaNumericString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		StringBuilder sb = new StringBuilder(length);

		for (int i = 0; i < length; i++) {
			sb.append(aphaNumericString.charAt(rd.nextInt(aphaNumericString.length())));
		}

		return sb.toString();
	}

	public static String generateRandomAlphaNumericString(int length) {
		Random rd = new Random();
		String alphaNumericString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		StringBuilder sb = new StringBuilder(length);

		for (int i = 0; i < length; i++) {
			sb.append(alphaNumericString.charAt(rd.nextInt(alphaNumericString.length())));
		}

		return sb.toString();
	}

	public static String createRandomNumber(long len) {
		if (len > 18) {
			throw new IllegalStateException("Too many digits");
		}
		long tLen = (long) Math.pow(10, len - 1) * 9;
		SecureRandom secureRandom = new SecureRandom();
		long number = secureRandom.nextLong(tLen) + (long) Math.pow(10, len - 1);

		String tVal = number + "";
		if (tVal.length() != len) {
			throw new IllegalStateException("The random number '" + tVal + "' is not '" + len + "' digits");
		}
		return tVal;
	}

	public static String getUniquePaymentNumber() {
		Random random = new Random(System.nanoTime());
		int randomInt = random.nextInt(1000000000) + 1;
		while (String.valueOf(randomInt).length() != 8) {
			randomInt = random.nextInt(1000000000) + 1;
		}
		String paymentNumber = generateRandomAlphabetsString(7).concat(String.valueOf(randomInt));
		return paymentNumber;
	}

	public static String getCurrentDate(String format) {
		// get current date
		Date today = new Date();
		DateFormat dateFormat = new SimpleDateFormat(format);
		dateFormat.setTimeZone(TimeZone.getTimeZone("CST6CDT"));
		Calendar cal = Calendar.getInstance();
		cal.add(Calendar.DAY_OF_MONTH, 0);
		today = cal.getTime();
		return dateFormat.format(today);
	}

	public static <T> void compareEquals(String what, T expected, T actual) {
		if (expected == null & actual == null) {
			log.info("Verification PASSED for " + what + "\nExpected :-->" + expected + "\nActual   :-->" + actual);
			return;
		}

		if (actual != null) {
			if (!actual.equals(expected)) {
				log.error(
						"Verification FAILED for " + what + "\nExpected :-->" + expected + "\nActual   :-->" + actual);
				Assertions.assertThat(actual).isEqualTo(expected);
			}
			else {
				log.info("Verification PASSED for " + what + "\nExpected :-->" + expected + "\nActual   :-->" + actual);
			}
		}
		else {
			log.error("Verification FAILED for " + what + "\nExpected :-->" + expected + "\nActual is null");
			Assertions.assertThat(actual).isEqualTo(expected);
		}
	}

	public static void compareContains(String what, String expected, String actual) {
		actual = actual.trim();
		if (actual != null) {
			if (actual.contains(expected.trim())) {
				log.info("Verification passed " + what + " Expected :-->" + expected + ">>>> Actual :-->" + actual);
			}
			else {
				log.error("Verification failed  " + what + " Expected :-->" + expected + ">>>> Actual :-->" + actual);
				Assertions.assertThat(actual).contains(expected);
			}
		}
	}

	public String[] getSegmentValues(String completeSegment, String segmentName) {
		String[] segments = completeSegment.split("~");

		for (String segment : segments) {
			String[] fields = segment.split("\\*");
			if (fields[0].equals(segmentName)) {
				return fields;
			}
		}
		return null;
	}

	public int getSegmentCount(String completeSegment, String segmentName) {
		String[] segments = completeSegment.split("~");
		int count = 0;
		for (String segment : segments) {
			String[] fields = segment.split("\\*");
			if (fields[0].equals(segmentName)) {
				count++;
			}
		}
		return count;
	}

	public List<String> getSTSESegmentValues(String completeSegment, String segmentName) {
		String[] segments = completeSegment.split("~");
		List<String> segmentValues = new ArrayList<>();
		for (String segment : segments) {
			String[] fields = segment.split("\\*");
			if (fields[0].equals(segmentName)) {
				segmentValues.add(segment);
			}
		}
		return segmentValues;
	}

	public String[] getSegmentValues(String completeSegment, String segment01, String segment02) {
		String[] segments = completeSegment.split("~");

		for (String segment : segments) {
			String[] fields = segment.split("\\*");
			if (fields[0].equals(segment01) && fields[1].equals(segment02)) {
				return fields;
			}
		}
		return null;
	}

	public Map<String, String[]> getPERSegmentValues(String completeSegment) {
		String[] segments = completeSegment.split("~");
		Map<String, String[]> PERsegments = new HashMap<>();
		int j = 1;
		for (int i = 0; i < segments.length; i++) {
			if (segments[i].equals("\nLX*" + (j))) {
				PERsegments.put("LX*" + (j), segments[i + 1].split("\\*"));
				j++;
			}
		}

		return PERsegments;
	}

	public String[] getPLBSegmentValues(String completeSegment) {
		String[] segments = completeSegment.split("~");
		ArrayList<String> PLBfields = new ArrayList<>();
		for (String segment : segments) {
			String[] fields = segment.split("\\*");
			if (fields[0].equals("\nPLB")) {
				PLBfields.addAll(List.of(fields));
			}
		}
		return PLBfields.toArray(new String[0]);
	}

	public String[] getCASSegmentValues(String input, String completeSegment) {
		String[] segments = completeSegment.split("~");
		String[] inputs = input.split("\\*");
		ArrayList<String> segmentValues = new ArrayList<>();
		for (String segment : segments) {
			String[] fields = segment.split("\\*");
			if (fields[0].equals(inputs[0]) && fields[1].equals(inputs[1])) {
				segmentValues.addAll(List.of(fields));
			}
		}
		return segmentValues.toArray(new String[0]);
	}

	public String getN1Details(String type, String completeSegment) {
		String[] segments = completeSegment.split("~");
		String payeeAddress = "";
		int i = 0;
		for (String segment : segments) {
			String[] fields = segment.split("\\*");
			if (fields[0].equals("\nN1") && fields[1].equals(type)) {
				break;
			}
			i++;
		}
		payeeAddress = segments[i] + segments[i + 1] + segments[i + 2];

		return payeeAddress;
	}

	public String getFormatedDate(String date) {
		return date.replace("-", "");
	}

	public String calculateSettlementDate(String numberOfDays) {
		Set<String> holidaySet = Stream.of(this.holidayDetails.split(REGEX_PIPE)).collect(Collectors.toSet());
		LocalDate settlementDate = LocalDate.now();
		int daysAdded = 0;

		try {
			int days = Integer.parseInt(numberOfDays);

			while (daysAdded <= days) {

				if (isValidSettlementDate(settlementDate, holidaySet)) {
					daysAdded++;
				}
				if (daysAdded > days) {
					break;
				}
				settlementDate = settlementDate.plusDays(1);
			}
		}
		catch (NumberFormatException ex) {
			throw new IllegalArgumentException("Invalid number of days: " + numberOfDays, ex);
		}

		DateTimeFormatter formatter = DateTimeFormatter.ofPattern("yyyy-MM-dd");
		return settlementDate.format(formatter);
	}

	private static boolean isValidSettlementDate(LocalDate date, Set<String> holidaySet) {
		String formattedDate = date.format(DateTimeFormatter.ofPattern("yyyy-MM-dd"));
		return !holidaySet.contains(formattedDate) && !isWeekend(date);
	}

	private static boolean isWeekend(LocalDate date) {
		DayOfWeek dayOfWeek = date.getDayOfWeek();
		return dayOfWeek == DayOfWeek.SATURDAY || dayOfWeek == DayOfWeek.SUNDAY;
	}

	public String getFileName(String shortCode) {
		return shortCode + "_" + getFormatedDate(currentDate) + "_01";
	}

	public static String getAmount(Double value) {
		if (value % 1 == 0) {
			return String.format("%.0f", value);
		}
		else {
			return String.valueOf(value);
		}
	}

	public static String getAmount(BigDecimal value) {
		if (value.remainder(BigDecimal.ONE).compareTo(BigDecimal.ZERO) == 0) {
			return String.format("%.0f", value);
		}
		else {
			return String.valueOf(value);
		}
	}

	public static void compareContainsAny(String field, String body, String... expectedFragments) {
		String lowerBody = body == null ? "" : body.toLowerCase();
		boolean matched = Arrays.stream(expectedFragments)
			.filter(Objects::nonNull)
			.map(String::toLowerCase)
			.anyMatch(lowerBody::contains);
		if (!matched) {
			throw new AssertionError("Field: " + field + "\nExpected any of: " + Arrays.toString(expectedFragments)
					+ "\nActual: " + body);
		}
	}

}
