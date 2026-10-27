package com.uhg.optum.financials.payments.payer.util;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.uhg.optum.financials.payments.payer.dto.CompanionDto;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;

class CompanionFileUtilTests {

	@Test
	@DisplayName("getCurrentDateTimeWithMillis returns formatted string of current date-time")
	void getCurrentDateTimeWithMillis_shouldReturnFormattedString() {
		// when
		String thisResult = CompanionFileUtil.getCurrentDateTimeWithMillis();
		// then
		// Should be 20 digits, all numeric
		assertThat(thisResult).hasSize(20).matches("\\d{20}");
	}

	@Test
	@DisplayName("getCompanionLineWithOutAmount serializes CompanionDto to JSON string")
	void getCompanionLineWithOutAmount_shouldSerializeDto() throws JsonProcessingException {
		// given
		CompanionDto thisDto = new CompanionDto();
		thisDto.setNetworkPaymentID("id123");
		thisDto.setPaymentType("I");
		// when
		String thisJson = CompanionFileUtil.getCompanionLineWithOutAmount(thisDto);
		// then
		assertThat(thisJson).contains("id123").contains("I");
	}

	@Test
	@DisplayName("getCompanionLineWithOutAmount returns 'null' for null input")
	void getCompanionLineWithOutAmount_shouldReturnNullStringForNullInput() throws JsonProcessingException {
		// given
		CompanionDto thisDto = null;
		// when
		String thisResult = CompanionFileUtil.getCompanionLineWithOutAmount(thisDto);
		// then
		assertThat(thisResult).isEqualTo("null");
	}

}
