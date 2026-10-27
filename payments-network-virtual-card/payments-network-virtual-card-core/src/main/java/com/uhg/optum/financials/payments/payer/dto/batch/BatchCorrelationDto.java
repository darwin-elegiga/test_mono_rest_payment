package com.uhg.optum.financials.payments.payer.dto.batch;

import io.swagger.v3.oas.annotations.media.Schema;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class BatchCorrelationDto {

	@Getter
	@Setter
	@Schema(example = "ORG001", description = "Organization code associated with the batch")

	private String org;

	@Getter
	@Setter
	@Schema(example = "CORR20250619ABC", description = "correlation ID for the batch")
	private String correlationId;

	@Getter
	@Setter
	@Schema(example = "3", description = "Number of batch")

	private int numberOfBatches;

	@Getter
	@Setter
	private List<BatchDto> batchDto;

}
