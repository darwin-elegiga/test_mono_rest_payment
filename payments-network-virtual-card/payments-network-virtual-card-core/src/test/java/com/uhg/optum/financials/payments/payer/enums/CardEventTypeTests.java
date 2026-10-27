package com.uhg.optum.financials.payments.payer.enums;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.assertj.core.api.Assertions;

@DisplayName("CardEventType Enum Tests")
class CardEventTypeTests {

	@Test
	@DisplayName("should contain all expected enum values")
	void shouldContainAllEnumValues() {
		Assertions.assertThat(CardEventType.valueOf("CREATE")).isEqualTo(CardEventType.CREATE);
		Assertions.assertThat(CardEventType.valueOf("CANCEL")).isEqualTo(CardEventType.CANCEL);
		Assertions.assertThat(CardEventType.valueOf("RESET")).isEqualTo(CardEventType.RESET);
		Assertions.assertThat(CardEventType.valueOf("AUTH")).isEqualTo(CardEventType.AUTH);
	}

	@Test
	@DisplayName("should return correct name for each enum value")
	void shouldReturnCorrectNameForEachEnum() {
		Assertions.assertThat(this.getEnumNames())
			.containsExactlyInAnyOrder("CREATE", "CANCEL", "RESET", "AUTH", "CLEARING");
	}

	@Test
	@DisplayName("should return all values in values() array")
	void shouldReturnAllValuesInArray() {
		CardEventType[] values = CardEventType.values();
		Assertions.assertThat(values)
			.containsExactly(CardEventType.CREATE, CardEventType.CANCEL, CardEventType.RESET, CardEventType.AUTH,
					CardEventType.CLEARING);
	}

	private String[] getEnumNames() {
		CardEventType[] values = CardEventType.values();
		String[] names = new String[values.length];
		for (int i = 0; i < values.length; i++) {
			names[i] = values[i].name();
		}
		return names;
	}

}
