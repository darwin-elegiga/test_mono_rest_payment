package com.uhg.optum.financials.payments.payer.dto;

import com.uhg.optum.financials.payments.organization.events.SubAccountUpdated;
import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

public class SubAccountUpdatedDto {

	@Getter
	@Setter
	private UUID id;

	@Getter
	@Setter
	private UUID tenantId;

	@Getter
	@Setter
	private SubAccountUpdated subAccountUpdated;

	@Getter
	@Setter
	private UUID eventId;

}
