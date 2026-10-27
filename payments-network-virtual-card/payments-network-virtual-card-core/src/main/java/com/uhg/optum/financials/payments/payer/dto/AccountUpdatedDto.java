package com.uhg.optum.financials.payments.payer.dto;

import com.uhg.optum.financials.payments.organization.events.AccountUpdated;
import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

public class AccountUpdatedDto {

	@Getter
	@Setter
	private UUID id;

	@Getter
	@Setter
	private UUID tenantId;

	@Getter
	@Setter
	private AccountUpdated accountupdated;

	@Getter
	@Setter
	private UUID eventId;

}
