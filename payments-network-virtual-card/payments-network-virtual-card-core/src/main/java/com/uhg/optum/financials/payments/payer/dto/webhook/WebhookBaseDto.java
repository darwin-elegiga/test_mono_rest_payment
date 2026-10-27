package com.uhg.optum.financials.payments.payer.dto.webhook;

import io.swagger.v3.oas.annotations.media.Schema;
import lombok.Getter;
import lombok.Setter;
import lombok.ToString;

import java.util.Map;

@ToString
public abstract class WebhookBaseDto {

	@Getter
	@Setter
	@Schema(description = "Webhook event type", example = "PAYMENT_CREATED")
	private String type;

	@Getter
	@Setter
	@Schema(description = "Timestamp of the event", example = "2025-10-01T12:00:00Z")
	private String timestamp;

	@Getter
	@Setter
	@Schema(description = "Unique identifier for the event", example = "event-12345")
	private Map<String, String> metadata;

	@Getter
	@Setter
	@Schema(description = "Error details if any", example = "{ \"code\": \"400\", \"message\": \"Invalid request\" }")
	private WebhookErrorPayload error;

	@Getter
	@Setter
	@Schema(description = "Payment ID", example = "123456789")
	private String paymentId;

	@Getter
	@Setter
	@Schema(description = "Network Payment ID", example = "987654321")
	private String networkPaymentId;

	@Getter
	@Setter
	private PaymentNetworkMetadataDto paymentNetworkMetadata;

}
