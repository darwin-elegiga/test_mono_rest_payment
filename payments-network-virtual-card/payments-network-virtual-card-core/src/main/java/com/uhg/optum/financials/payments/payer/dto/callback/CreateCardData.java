package com.uhg.optum.financials.payments.payer.dto.callback;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;
import java.util.List;
import java.util.UUID;

@Getter
@Setter
public class CreateCardData implements Serializable {

	private static final long serialVersionUID = 1L;

	private UUID clientRequestId;

	private UUID cardGuid;

	private InitialBalance initialBalance;

	private List<String> spendRules;

	private Boolean singleSpend;

	private Boolean autoActivateCard;

	@Getter
	@Setter
	public static class InitialBalance implements Serializable {

		private static final long serialVersionUID = 1L;

		private Long amount;

		private String currency;

	}

}
