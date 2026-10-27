package com.uhg.optum.financials.payments.payer.util;

import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;

class ParameterProviderTests {

	@Test
	void getValueReturnsCorrectValueForValidKey() {
		String jsonString = "{\"key\":\"value\"}";
		String result = ParameterProvider.getValue(jsonString, "key");
		Assertions.assertThat(result).isEqualTo("value");
	}

	@Test
	void getValueReturnsNullForNonExistentKey() {
		String jsonString = "{\"key\":\"value\"}";
		String result = ParameterProvider.getValue(jsonString, "nonExistentKey");
		Assertions.assertThat(result).isNull();
	}

	@Test
	void getValueReturnsNullForInvalidJson() {
		String jsonString = "invalidJson";
		String result = ParameterProvider.getValue(jsonString, "key");
		Assertions.assertThat(result).isNull();
	}

	@Test
	void getBooleanValueReturnsTrueForValidTrueValue() {
		String jsonString = "{\"key\":\"true\"}";
		boolean result = ParameterProvider.getBooleanValue(jsonString, "key");
		Assertions.assertThat(result).isTrue();
	}

	@Test
	void getBooleanValueReturnsFalseForValidFalseValue() {
		String jsonString = "{\"key\":\"false\"}";
		boolean result = ParameterProvider.getBooleanValue(jsonString, "key");
		Assertions.assertThat(result).isFalse();
	}

	@Test
	void getBooleanValueReturnsFalseForNonExistentKey() {
		String jsonString = "{\"key\":\"true\"}";
		boolean result = ParameterProvider.getBooleanValue(jsonString, "nonExistentKey");
		Assertions.assertThat(result).isFalse();
	}

	@Test
	void getBooleanValueReturnsFalseForInvalidJson() {
		String jsonString = "invalidJson";
		boolean result = ParameterProvider.getBooleanValue(jsonString, "key");
		Assertions.assertThat(result).isFalse();
	}

	@Test
	void toJsonStringReturnsValidJsonForObject() {
		Object obj = new Object() {
			public String key = "value";

		};
		String result = ParameterProvider.toJsonString(obj);
		Assertions.assertThat(result).isEqualTo("{\"key\":\"value\"}");
	}

	@Test
	void toJsonStringReturnsNullForNullObject() {
		Object obj = new Object() {
			public Object key = new Object(); // Causes JsonProcessingException

		};
		String result = ParameterProvider.toJsonString(obj);
		Assertions.assertThat(result).isNull();
	}

}
