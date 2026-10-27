package com.uhg.optum.financials.payments.payer.enums;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.assertj.core.api.Assertions;

@DisplayName("LogoType Enum Tests")
class LogoTypeTests {

	@Test
	@DisplayName("should contain all expected enum values")
	void shouldContainAllEnumValues() {
		Assertions.assertThat(LogoType.valueOf("MASTERCARD")).isEqualTo(LogoType.MASTERCARD);
		Assertions.assertThat(LogoType.valueOf("VISA")).isEqualTo(LogoType.VISA);
		Assertions.assertThat(LogoType.valueOf("OPTUM")).isEqualTo(LogoType.OPTUM);
		Assertions.assertThat(LogoType.valueOf("INSTRUCTIONS")).isEqualTo(LogoType.INSTRUCTIONS);
	}

	@Test
	@DisplayName("should return correct name for each enum value")
	void shouldReturnCorrectNameForEachEnum() {
		Assertions.assertThat(this.getEnumNames())
			.containsExactlyInAnyOrder("MASTERCARD", "VISA", "OPTUM", "INSTRUCTIONS");
	}

	@Test
	@DisplayName("should return all values in values() array")
	void shouldReturnAllValuesInArray() {
		LogoType[] values = LogoType.values();
		Assertions.assertThat(values)
			.containsExactly(LogoType.MASTERCARD, LogoType.VISA, LogoType.OPTUM, LogoType.INSTRUCTIONS);
	}

	private String[] getEnumNames() {
		LogoType[] values = LogoType.values();
		String[] names = new String[values.length];
		for (int i = 0; i < values.length; i++) {
			names[i] = values[i].name();
		}
		return names;
	}

}
