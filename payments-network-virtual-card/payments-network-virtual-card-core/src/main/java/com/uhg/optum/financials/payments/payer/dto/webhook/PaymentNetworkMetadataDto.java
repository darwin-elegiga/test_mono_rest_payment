package com.uhg.optum.financials.payments.payer.dto.webhook;

import lombok.Getter;
import lombok.Setter;

import java.util.Map;

@Getter
@Setter
public class PaymentNetworkMetadataDto {

	private String modality;

	private CardWebhookDto card;

	private Map<String, String> networkMetadata;

}
