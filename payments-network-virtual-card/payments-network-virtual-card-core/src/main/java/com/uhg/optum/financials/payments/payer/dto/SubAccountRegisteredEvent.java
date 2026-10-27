package com.uhg.optum.financials.payments.payer.dto;

import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

public class SubAccountRegisteredEvent {

	@Getter
	@Setter
	private UUID id;

	@Getter
	@Setter
	private UUID tenantId;

	@Getter
	@Setter
	private SubAccountRegistered subAccountRegistered;

	@Getter
	@Setter
	private UUID eventId;

}
