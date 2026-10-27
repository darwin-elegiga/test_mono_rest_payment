package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.Consolidated835FileDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookBaseDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.WebhookBaseDto;

public interface WebhookClient {

	<T extends WebhookBaseDto> void notifyClient(T data, String endpoint);

	<T extends WebhookBaseDto> void notifyClient(T data, String tenantId, String endpoint);

	<T extends CardEventWebhookBaseDto> void notifyClient(T data, String tenantId, String endpoint);

	<T extends WebhookBaseDto> boolean notifyClientWithStatus(T data, String tenantId, String endpoint);

	boolean notifyRemittanceClient(Consolidated835FileDto consolidated835FileDto, String endpoint);

}
