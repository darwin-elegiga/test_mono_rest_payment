package com.uhg.optum.financials.payments.payer.dto.webhook;

import lombok.Getter;
import lombok.Setter;

import java.math.BigDecimal;

public class AuthorizationWebhookPayload {

	@Getter
	@Setter
	private String receiptId;

	@Getter
	@Setter
	private String approvalCode;

	@Getter
	@Setter
	private String authExpiryTime;

	@Getter
	@Setter
	private BigDecimal authAmount;

	@Getter
	@Setter
	private String accountingTransaction;

	@Getter
	@Setter
	private String responseCode;

	@Getter
	@Setter
	private String declineReason;

	@Getter
	@Setter
	private MerchantPayload merchant;

}
