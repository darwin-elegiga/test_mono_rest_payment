package com.uhg.optum.financials.payments.payer.dto.callback;

import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

@Getter
@Setter
public class ResetCardData {

	private String receiptId;

	private UUID cardGuid;

	private String resetPurpose;

	private Balance balanceBeforeReset;

	private Balance balanceAfterReset;

	@Getter
	@Setter
	public static class Balance {

		private int amount;

		private String currency;

	}

}
