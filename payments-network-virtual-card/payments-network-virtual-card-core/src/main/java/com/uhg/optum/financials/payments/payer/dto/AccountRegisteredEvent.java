package com.uhg.optum.financials.payments.payer.dto;

import com.uhg.optum.financials.payments.organization.events.AccountRegistered;
import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

public class AccountRegisteredEvent {

	@Getter
	@Setter
	private UUID id;

	@Getter
	@Setter
	private UUID tenantId;

	@Getter
	@Setter
	private AccountRegistered accountRegistered;

	@Getter
	@Setter
	private UUID eventId;

}
