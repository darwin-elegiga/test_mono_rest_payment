package com.uhg.optum.financials.payments.payer.enums;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.assertj.core.api.Assertions;

@DisplayName("VirtualCardState Enum Tests")
class VirtualCardStateTests {

	@Test
	@DisplayName("should contain all expected enum values")
	void shouldContainAllEnumValues() {
		Assertions.assertThat(VirtualCardState.valueOf("CREATED")).isEqualTo(VirtualCardState.CREATED);
		Assertions.assertThat(VirtualCardState.valueOf("CANCELLED")).isEqualTo(VirtualCardState.CANCELLED);
		Assertions.assertThat(VirtualCardState.valueOf("RESET")).isEqualTo(VirtualCardState.RESET);
	}

	@Test
	@DisplayName("should return correct name for each enum value")
	void shouldReturnCorrectNameForEachEnum() {
		Assertions.assertThat(this.getEnumNames()).containsExactlyInAnyOrder("CREATED", "CANCELLED", "RESET");
	}

	@Test
	@DisplayName("should return all values in values() array")
	void shouldReturnAllValuesInArray() {
		VirtualCardState[] values = VirtualCardState.values();
		Assertions.assertThat(values)
			.containsExactly(VirtualCardState.CREATED, VirtualCardState.CANCELLED, VirtualCardState.RESET);
	}

	private String[] getEnumNames() {
		VirtualCardState[] values = VirtualCardState.values();
		String[] names = new String[values.length];
		for (int i = 0; i < values.length; i++) {
			names[i] = values[i].name();
		}
		return names;
	}

}
