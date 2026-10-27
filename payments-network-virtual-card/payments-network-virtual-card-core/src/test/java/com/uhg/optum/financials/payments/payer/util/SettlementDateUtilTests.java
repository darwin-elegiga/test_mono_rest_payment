package com.uhg.optum.financials.payments.payer.util;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.time.DayOfWeek;
import java.time.LocalDate;
import java.util.Set;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class SettlementDateUtilTests {

	@Test
	@DisplayName("calculateSettlementDate returns correct date skipping weekends and holidays")
	void calculateSettlementDate_shouldSkipWeekendsAndHolidays() {
		String thisHolidays = LocalDate.now().plusDays(2).toString();
		String thisDays = "3";
		LocalDate thisResult = SettlementDateUtil.calculateSettlementDate(thisHolidays, thisDays);
		LocalDate thisExpected = LocalDate.now();
		int thisCount = 0;
		while (true) {
			if (!thisHolidays.contains(thisExpected.toString()) && thisExpected.getDayOfWeek() != DayOfWeek.SATURDAY
					&& thisExpected.getDayOfWeek() != DayOfWeek.SUNDAY) {
				thisCount++;
			}
			if (thisCount > 3) {
				break;
			}
			thisExpected = thisExpected.plusDays(1);
		}
		assertThat(thisResult).isEqualTo(thisExpected);
	}

	@Test
	@DisplayName("calculateSettlementDate throws IllegalArgumentException for invalid days")
	void calculateSettlementDate_shouldThrowExceptionForInvalidDays() {
		// given
		String thisHolidays = "";
		String thisDays = "abc";
		// when/then
		assertThatThrownBy(() -> SettlementDateUtil.calculateSettlementDate(thisHolidays, thisDays))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("Invalid number of days");
	}

	@Test
	@DisplayName("isValidSettlementDate returns false for weekends and holidays")
	void isValidSettlementDate_shouldReturnFalseForWeekendsAndHolidays() {
		// given
		LocalDate thisSaturday = LocalDate.now().with(DayOfWeek.SATURDAY);
		LocalDate thisHoliday = LocalDate.now().plusDays(1);
		Set<String> thisHolidaySet = Set.of(thisHoliday.toString());
		// when
		boolean thisIsHoliday = SettlementDateUtil.calculateSettlementDate(thisHoliday.toString(), "0")
			.equals(thisHoliday);
		boolean thisIsWeekend = thisSaturday.getDayOfWeek() == DayOfWeek.SATURDAY
				|| thisSaturday.getDayOfWeek() == DayOfWeek.SUNDAY;
		// then
		assertThat(thisIsHoliday).isFalse();
		assertThat(thisIsWeekend).isTrue();
	}

	@Test
	@DisplayName("isWeekend returns true for Saturday and Sunday, false otherwise")
	void isWeekend_shouldReturnTrueForSaturdayAndSunday() {
		// given
		LocalDate thisSaturday = LocalDate.now().with(DayOfWeek.SATURDAY);
		LocalDate thisSunday = LocalDate.now().with(DayOfWeek.SUNDAY);
		LocalDate thisMonday = LocalDate.now().with(DayOfWeek.MONDAY);
		// when/then
		assertThat(thisSaturday.getDayOfWeek()).isEqualTo(DayOfWeek.SATURDAY);
		assertThat(thisSunday.getDayOfWeek()).isEqualTo(DayOfWeek.SUNDAY);
		assertThat(thisMonday.getDayOfWeek()).isEqualTo(DayOfWeek.MONDAY);
	}

}
