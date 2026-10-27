package com.uhg.optum.financials.payments.payer.enums;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.assertj.core.api.Assertions;

@DisplayName("OrganizationStatus Enum Tests")
class OrganizationStatusTests {

	@Test
	@DisplayName("should contain all expected enum values")
	void shouldContainAllEnumValues() {
		Assertions.assertThat(OrganizationStatus.valueOf("ACTIVE")).isEqualTo(OrganizationStatus.ACTIVE);
		Assertions.assertThat(OrganizationStatus.valueOf("CLOSED")).isEqualTo(OrganizationStatus.CLOSED);
	}

	@Test
	@DisplayName("should return correct name for each enum value")
	void shouldReturnCorrectNameForEachEnum() {
		Assertions.assertThat(this.getEnumNames()).containsExactlyInAnyOrder("ACTIVE", "CLOSED");
	}

	@Test
	@DisplayName("should return all values in values() array")
	void shouldReturnAllValuesInArray() {
		OrganizationStatus[] values = OrganizationStatus.values();
		Assertions.assertThat(values).containsExactly(OrganizationStatus.ACTIVE, OrganizationStatus.CLOSED);
	}

	@Test
	@DisplayName("should create enum from string using fromString (case insensitive)")
	void shouldCreateEnumFromStringCaseInsensitive() {
		Assertions.assertThat(OrganizationStatus.fromString("active")).isEqualTo(OrganizationStatus.ACTIVE);
		Assertions.assertThat(OrganizationStatus.fromString("CLOSED")).isEqualTo(OrganizationStatus.CLOSED);
	}

	@Test
	@DisplayName("should throw IllegalArgumentException for invalid value in fromString")
	void shouldThrowExceptionForInvalidValue() {
		Assertions.assertThatThrownBy(() -> OrganizationStatus.fromString("INVALID"))
			.isInstanceOf(IllegalArgumentException.class);
	}

	private String[] getEnumNames() {
		OrganizationStatus[] values = OrganizationStatus.values();
		String[] names = new String[values.length];
		for (int i = 0; i < values.length; i++) {
			names[i] = values[i].name();
		}
		return names;
	}

}
