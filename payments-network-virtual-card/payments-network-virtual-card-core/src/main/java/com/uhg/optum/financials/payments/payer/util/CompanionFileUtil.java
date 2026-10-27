package com.uhg.optum.financials.payments.payer.util;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.json.JsonMapper;
import com.uhg.optum.financials.payments.payer.dto.CompanionDto;

import java.time.Instant;

public final class CompanionFileUtil {

	private static final JsonMapper JSON_MAPPER = new JsonMapper();

	private CompanionFileUtil() {
	}

	public static String getCurrentDateTimeWithMillis() {
		return Instant.now().toString().replaceAll("[-T:Z.]", "").substring(0, 20);
	}

	public static String getCompanionLineWithOutAmount(CompanionDto companionLine) throws JsonProcessingException {
		return JSON_MAPPER.writeValueAsString(companionLine);
	}

}
