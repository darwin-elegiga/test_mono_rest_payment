package com.uhg.optum.financials.payments.payer.dto;

import com.fasterxml.jackson.annotation.JsonFormat;
import lombok.Data;

import java.math.BigDecimal;
import java.util.List;

@Data
public class CardCreationStatusDto {

	private String clientRequestId;

	private String status;

	private String createdAt;

	private String completedAt;

	private CardData data;

	private Object error;

	@Data
	public static class CardData {

		private String cardGuid;

		private InitialBalance initialBalance;

		private List<String> spendRules;

		private Boolean singleSpend;

		private Boolean autoActivateCard;

	}

	@Data
	public static class InitialBalance {

		private String currency;

		@JsonFormat(shape = JsonFormat.Shape.STRING)
		private BigDecimal amount;

	}

}
