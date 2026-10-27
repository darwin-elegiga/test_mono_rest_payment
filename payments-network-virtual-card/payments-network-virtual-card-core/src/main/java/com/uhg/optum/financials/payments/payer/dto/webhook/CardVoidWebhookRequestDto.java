package com.uhg.optum.financials.payments.payer.dto.webhook;

import lombok.Getter;
import lombok.Setter;

import java.math.BigDecimal;
import java.util.UUID;

@Getter
@Setter
public class CardVoidWebhookRequestDto {

	private UUID cardId;

	private String status;

	private BigDecimal amount;

	private String timeStamp;

}
