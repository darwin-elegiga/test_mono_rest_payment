package com.uhg.optum.financials.payments.payer.util;

import lombok.extern.slf4j.Slf4j;

import java.time.DayOfWeek;
import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.util.Set;
import java.util.stream.Collectors;
import java.util.stream.Stream;

import static com.uhg.optum.financials.shared.core.service.SettlementDateUtil.DATE_FORMAT_YYYY_MM_DD;
import static com.uhg.optum.financials.shared.core.service.SettlementDateUtil.REGEX_PIPE;

@Slf4j
public final class SettlementDateUtil {

	private SettlementDateUtil() {
		// Private constructor to prevent instantiation
	}

	public static LocalDate calculateSettlementDate(String holidaysList, String numberOfDays, LocalDate date) {
		Set<String> holidaySet = Stream.of(holidaysList.split(REGEX_PIPE)).collect(Collectors.toSet());

		LocalDate settlementDate = date;
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

		return settlementDate;
	}

	private static boolean isValidSettlementDate(LocalDate date, Set<String> holidaySet) {
		String formattedDate = date.format(DateTimeFormatter.ofPattern(DATE_FORMAT_YYYY_MM_DD));
		return !holidaySet.contains(formattedDate) && !isWeekend(date);
	}

	private static boolean isWeekend(LocalDate date) {
		DayOfWeek dayOfWeek = date.getDayOfWeek();
		return dayOfWeek == DayOfWeek.SATURDAY || dayOfWeek == DayOfWeek.SUNDAY;
	}

}
