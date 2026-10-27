package com.uhg.optum.financials.payments.payer.dto.webhook;

import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

@Getter
@Setter
public class CardWebhookDto {

	private UUID id;

	private String status;

	private String bin;

	private String last4;

	private String receiptId;

	private String accountingTransaction;

	private String approvalCode;

	private String merchantName;

	private String merchantTerminalId;

	private Balance balance;

}
