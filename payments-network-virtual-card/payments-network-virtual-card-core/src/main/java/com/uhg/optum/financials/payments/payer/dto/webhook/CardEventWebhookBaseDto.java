package com.uhg.optum.financials.payments.payer.dto.webhook;

import com.fasterxml.jackson.annotation.JsonProperty;
import io.swagger.v3.oas.annotations.media.Schema;
import lombok.Getter;
import lombok.Setter;

import java.util.Map;

public class CardEventWebhookBaseDto {

	@Getter
	@Setter
	@Schema(description = "Timestamp of the event", example = "2025-10-01T12:00:00Z")
	private String timestamp;

	@Getter
	@Setter
	@Schema(description = "Network Payment ID", example = "987654321")
	private String networkPaymentId;

	@Getter
	@Setter
	private String networkId;

	@Getter
	@Setter
	@Schema(description = "Payment ID", example = "123456789")
	private String paymentId;

	@Getter
	@Setter
	@Schema(description = "Webhook event type", example = "PAYMENT_CREATED")
	private String type;

	@Getter
	@Setter
	@JsonProperty("isPublic")
	private boolean isPublic;

	@Getter
	@Setter
	private boolean containsPCI;

	@Getter
	@Setter
	private String modality;

	@Getter
	@Setter
	private Map<String, String> networkMetadata;

	@Getter
	@Setter
	private CardEventWebhookPayload card;

}
