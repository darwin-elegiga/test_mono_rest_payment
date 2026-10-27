package com.uhg.optum.financials.payments.payer.util;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import lombok.extern.slf4j.Slf4j;

@Slf4j
public final class ParameterProvider {

	private static final ObjectMapper mapper = new ObjectMapper()
		.setSerializationInclusion(JsonInclude.Include.NON_NULL);

	private ParameterProvider() {

	}

	public static String getValue(String jsonString, String key) {
		try {
			ObjectMapper mapper = new ObjectMapper();
			JsonNode root = mapper.readTree(jsonString);
			return root.get(key).asText();
		}
		catch (Exception ex) {
			return null;
		}
	}

	public static boolean getBooleanValue(String jsonString, String key) {
		String value = getValue(jsonString, key);
		if (value == null || value.isEmpty()) {
			return false;
		}
		return Boolean.parseBoolean(value);
	}

	public static String toJsonString(Object obj) {
		try {
			return mapper.writeValueAsString(obj);
		}
		catch (JsonProcessingException ex) {
			log.warn("Error converting object to JSON string: {}", ex.getMessage());
			return null;
		}
	}

}
