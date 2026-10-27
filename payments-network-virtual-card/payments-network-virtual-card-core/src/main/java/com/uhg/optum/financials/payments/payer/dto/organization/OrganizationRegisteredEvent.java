package com.uhg.optum.financials.payments.payer.dto.organization;

import com.uhg.optum.financials.payments.organization.events.OrganizationRegistered;
import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

public class OrganizationRegisteredEvent {

	@Getter
	@Setter
	private UUID id;

	@Getter
	@Setter
	private UUID tenantId;

	@Getter
	@Setter
	private OrganizationRegistered organizationRegistered;

	@Getter
	@Setter
	private UUID eventId;

}
