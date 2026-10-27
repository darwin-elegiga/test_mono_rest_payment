package com.uhg.optum.financials.payments.payer.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;

class FileTrackingDtoTests {

	@Test
	void testFileTrackingDtoSettersAndGetters() {
		FileTrackingDto dto = new FileTrackingDto();
		dto.set_835("file835");
		dto.setCompanion("companionFile");
		dto.setCorrelationId("corrId");
		Assertions.assertThat(dto.get_835()).isEqualTo("file835");
		Assertions.assertThat(dto.getCompanion()).isEqualTo("companionFile");
		Assertions.assertThat(dto.getCorrelationId()).isEqualTo("corrId");
	}

	@Test
	void serializes835AsJsonProperty() throws Exception {
		FileTrackingDto dto = new FileTrackingDto();
		dto.set_835("file835");
		dto.setCompanion("companionFile");
		dto.setCorrelationId("corrId");
		ObjectMapper objectMapper = new ObjectMapper();

		JsonNode json = objectMapper.readTree(objectMapper.writeValueAsString(dto));

		Assertions.assertThat(json.get("835").asText()).isEqualTo("file835");
		Assertions.assertThat(json.has("_835")).isFalse();
		Assertions.assertThat(json.get("companion").asText()).isEqualTo("companionFile");
		Assertions.assertThat(json.get("correlationId").asText()).isEqualTo("corrId");
	}

	@Test
	void serializesNullFileTrackingValues() throws Exception {
		FileTrackingDto dto = new FileTrackingDto();
		dto.set_835(null);
		dto.setCompanion(null);
		dto.setCorrelationId(null);
		ObjectMapper objectMapper = new ObjectMapper().setDefaultPropertyInclusion(JsonInclude.Include.NON_NULL);

		JsonNode json = objectMapper.readTree(objectMapper.writeValueAsString(dto));

		Assertions.assertThat(json.has("835")).isTrue();
		Assertions.assertThat(json.get("835").isNull()).isTrue();
		Assertions.assertThat(json.has("companion")).isTrue();
		Assertions.assertThat(json.get("companion").isNull()).isTrue();
		Assertions.assertThat(json.has("correlationId")).isTrue();
		Assertions.assertThat(json.get("correlationId").isNull()).isTrue();
	}

	@Test
	void serializes835WithJackson3Mapper() throws Exception {
		FileTrackingDto dto = new FileTrackingDto();
		dto.set_835("file835");
		dto.setCompanion("companionFile");
		dto.setCorrelationId("corrId");
		tools.jackson.databind.json.JsonMapper objectMapper = new tools.jackson.databind.json.JsonMapper();

		tools.jackson.databind.JsonNode json = objectMapper.readTree(objectMapper.writeValueAsString(dto));

		Assertions.assertThat(json.get("835").asString()).isEqualTo("file835");
		Assertions.assertThat(json.has("_835")).isFalse();
		Assertions.assertThat(json.get("companion").asString()).isEqualTo("companionFile");
		Assertions.assertThat(json.get("correlationId").asString()).isEqualTo("corrId");
	}

	@Test
	void serializesNullValuesWithJackson3Mapper() throws Exception {
		FileTrackingDto dto = new FileTrackingDto();
		tools.jackson.databind.json.JsonMapper objectMapper = tools.jackson.databind.json.JsonMapper.builder()
			.changeDefaultPropertyInclusion((existing) -> JsonInclude.Value.construct(JsonInclude.Include.NON_NULL,
					JsonInclude.Include.NON_NULL))
			.build();

		tools.jackson.databind.JsonNode json = objectMapper.readTree(objectMapper.writeValueAsString(dto));

		Assertions.assertThat(json.has("835")).isTrue();
		Assertions.assertThat(json.get("835").isNull()).isTrue();
		Assertions.assertThat(json.has("companion")).isTrue();
		Assertions.assertThat(json.get("companion").isNull()).isTrue();
		Assertions.assertThat(json.has("correlationId")).isTrue();
		Assertions.assertThat(json.get("correlationId").isNull()).isTrue();
	}

}
