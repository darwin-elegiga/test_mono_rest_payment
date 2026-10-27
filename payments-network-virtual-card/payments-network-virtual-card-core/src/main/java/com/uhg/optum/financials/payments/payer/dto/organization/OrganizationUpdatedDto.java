package com.uhg.optum.financials.payments.payer.dto.organization;

import com.uhg.optum.financials.payments.organization.events.OrganizationUpdated;
import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

public class OrganizationUpdatedDto {

	@Getter
	@Setter
	private UUID id;

	@Getter
	@Setter
	private UUID tenantId;

	@Getter
	@Setter
	private OrganizationUpdated organizationUpdated;

	@Getter
	@Setter
	private UUID eventId;

}
