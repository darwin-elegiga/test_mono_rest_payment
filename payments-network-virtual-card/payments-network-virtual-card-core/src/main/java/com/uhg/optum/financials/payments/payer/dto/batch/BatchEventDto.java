package com.uhg.optum.financials.payments.payer.dto.batch;

import io.swagger.v3.oas.annotations.media.Schema;
import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

@Getter
@Setter
public class BatchEventDto {

	@Getter
	@Setter
	@Schema(description = "Unique identifier for the batch event", example = "123e4567-e89b-12d3-a456-426614174000")
	private String id;

	@Getter
	@Setter
	@Schema(description = "Tenant ID", example = "123e4567-e89b-12d3-a456-426614174001")
	private UUID tenantId;

	@Getter
	@Setter
	private CorrelationDto correlationDto;

	@Getter
	@Setter
	@Schema(description = "Event ID", example = "123e4567-e89b-12d3-a456-426614174002")
	private UUID eventId;

}
