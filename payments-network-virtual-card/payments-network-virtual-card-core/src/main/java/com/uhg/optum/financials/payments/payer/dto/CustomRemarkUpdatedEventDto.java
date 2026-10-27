package com.uhg.optum.financials.payments.payer.dto;

import com.uhg.optum.financials.payments.remittance.events.CustomRemarkUpdated;
import io.swagger.v3.oas.annotations.media.Schema;
import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

@Getter
@Setter
public class CustomRemarkUpdatedEventDto {

	@Getter
	@Setter
	@Schema(description = "Unique identifier for the custom remarks event",
			example = "123e4567-e89b-12d3-a456-426614174000")
	private UUID id;

	@Getter
	@Setter
	@Schema(description = "Tenant ID", example = "123e4567-e89b-12d3-a456-426614174001")
	private UUID tenantId;

	@Getter
	@Setter
	private CustomRemarkUpdated customRemarkUpdated;

	@Getter
	@Setter
	@Schema(description = "Event ID", example = "123e4567-e89b-12d3-a456-426614174002")
	private UUID eventId;

}
