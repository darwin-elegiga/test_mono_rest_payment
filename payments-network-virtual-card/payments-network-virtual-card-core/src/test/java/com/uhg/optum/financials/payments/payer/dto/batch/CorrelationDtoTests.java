package com.uhg.optum.financials.payments.payer.dto.batch;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import static org.assertj.core.api.BDDAssertions.then;
import java.util.ArrayList;
import java.util.List;

class CorrelationDtoTests {

	@Test
	@DisplayName("should set and get all fields correctly")
	void shouldSetAndGetAllFields() {
		// Arrange
		CorrelationDto correlationDto = new CorrelationDto();
		String org = "ORG01";
		String correlationId = "123e4567-e89b-12d3-a456-426614174000";
		int numberOfBatches = 2;
		List<BatchDetailDto> batches = new ArrayList<>();
		String type = "batch_correlated";

		// Act
		correlationDto.setOrg(org);
		correlationDto.setCorrelationId(correlationId);
		correlationDto.setNumberOfBatches(numberOfBatches);
		correlationDto.setBatches(batches);
		correlationDto.setType(type);

		// Assert
		then(correlationDto.getOrg()).isEqualTo(org);
		then(correlationDto.getCorrelationId()).isEqualTo(correlationId);
		then(correlationDto.getNumberOfBatches()).isEqualTo(numberOfBatches);
		then(correlationDto.getBatches()).isEqualTo(batches);
		then(correlationDto.getType()).isEqualTo(type);
		then(correlationDto.type()).isEqualTo(type);
	}

	@Test
	@DisplayName("should use all-args constructor and this keyword for local variables")
	void shouldUseAllArgsConstructorAndThisKeyword() {
		// Arrange
		String org = "ORG02";
		String correlationId = "abc123";
		int numberOfBatches = 3;
		List<BatchDetailDto> batches = new ArrayList<>();
		String type = "batch_correlated";
		CorrelationDto correlationDto = new CorrelationDto(org, correlationId, numberOfBatches, batches, type);

		// Assert
		then(correlationDto.getOrg()).isEqualTo(org);
		then(correlationDto.getCorrelationId()).isEqualTo(correlationId);
		then(correlationDto.getNumberOfBatches()).isEqualTo(numberOfBatches);
		then(correlationDto.getBatches()).isEqualTo(batches);
		then(correlationDto.getType()).isEqualTo(type);
		then(correlationDto.type()).isEqualTo(type);
	}

}
