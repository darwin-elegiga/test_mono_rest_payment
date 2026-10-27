package com.uhg.optum.financials.payments.payer.dto.webhook;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class LoadingWebhookRequestPayload {

	@Getter
	@Setter
	private List<DocumentPayload> documents;

	@Getter
	@Setter
	private PaymentActivityPayload paymentActivity;

	@Getter
	@Setter
	private String expirationDate;

	@Getter
	@Setter
	private String networkId;

}
