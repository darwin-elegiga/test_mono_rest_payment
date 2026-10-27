package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.Consolidated835FileDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookBaseDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.WebhookBaseDto;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.FOR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INVOKING_WEBHOOK_ENDPOINT;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.WITH_TENANT_ID;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.DEBUG;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.ERROR;

@Slf4j
public class WebhookClientImpl implements WebhookClient {

	private final LogUtility logUtility;

	public static final String X_OPTUM_TENANT_ID = "x-optum-tenant-id";

	private final ResilientRestClient webhookClient;

	public WebhookClientImpl(ResilientRestClientFactory resilientRestClientFactory, LogUtility logUtility) {
		this(resilientRestClientFactory.getClient("webhook"), logUtility);
	}

	public WebhookClientImpl(ResilientRestClient webhookClient, LogUtility logUtility) {
		this.webhookClient = webhookClient;
		this.logUtility = logUtility;
	}

	@Override
	public <T extends WebhookBaseDto> void notifyClient(T data, String endpoint) {
		callEndpoint(data, null, endpoint);
	}

	@Override
	public <T extends WebhookBaseDto> void notifyClient(T data, String tenantId, String endpoint) {
		callEndpoint(data, tenantId, endpoint);
	}

	@Override
	public <T extends CardEventWebhookBaseDto> void notifyClient(T data, String tenantId, String endpoint) {
		callEndpoint(data, tenantId, endpoint);
	}

	@Override
	public <T extends WebhookBaseDto> boolean notifyClientWithStatus(T data, String tenantId, String endpoint) {
		return callEndpointWithStatus(data, tenantId, endpoint);
	}

	@Override
	public boolean notifyRemittanceClient(Consolidated835FileDto consolidated835FileDto, String endpoint) {
		HttpHeaders headers = new HttpHeaders();
		headers.setContentType(MediaType.APPLICATION_JSON);
		String successMsg = "Invoking webhook endpoint: " + endpoint + " and Payload is: " + consolidated835FileDto;
		return post(consolidated835FileDto, endpoint, headers, successMsg, null, null);
	}

	private <T extends WebhookBaseDto> boolean callEndpointWithStatus(T data, String tenantId, String endpoint) {
		HttpHeaders headers = new HttpHeaders();
		headers.setContentType(MediaType.APPLICATION_JSON);
		if (tenantId != null && !tenantId.isEmpty()) {
			headers.add(X_OPTUM_TENANT_ID, tenantId);
		}
		String sanitizedTenantId = (tenantId != null) ? tenantId.replace("\n", "").replace("\r", "") : null;
		String eventType = data.getType();
		String paymentId = data.getPaymentId();
		String successMsg = INVOKING_WEBHOOK_ENDPOINT + endpoint + FOR + eventType + WITH_TENANT_ID + sanitizedTenantId;
		return post(data, endpoint, headers, successMsg, eventType, paymentId);
	}

	private <T extends WebhookBaseDto> void callEndpoint(T data, String tenantId, String endpoint) {
		HttpHeaders headers = new HttpHeaders();
		headers.setContentType(MediaType.APPLICATION_JSON);
		if (tenantId != null && !tenantId.isEmpty()) {
			headers.add(X_OPTUM_TENANT_ID, tenantId);
		}
		String sanitizedTenantId = (tenantId != null) ? tenantId.replace("\n", "").replace("\r", "") : null;
		String eventType = data.getType();
		String paymentId = data.getPaymentId();
		String successMsg = INVOKING_WEBHOOK_ENDPOINT + endpoint + FOR + eventType + WITH_TENANT_ID + sanitizedTenantId;
		post(data, endpoint, headers, successMsg, eventType, paymentId);
	}

	private <T extends CardEventWebhookBaseDto> void callEndpoint(T data, String tenantId, String endpoint) {
		HttpHeaders headers = new HttpHeaders();
		headers.setContentType(MediaType.APPLICATION_JSON);
		if (tenantId != null && !tenantId.isEmpty()) {
			headers.add(X_OPTUM_TENANT_ID, tenantId);
		}
		String sanitizedTenantId = (tenantId != null) ? tenantId.replace("\n", "").replace("\r", "") : null;
		String eventType = data.getType();
		String paymentId = data.getPaymentId();
		String successMsg = INVOKING_WEBHOOK_ENDPOINT + endpoint + FOR + eventType + WITH_TENANT_ID + sanitizedTenantId;
		post(data, endpoint, headers, successMsg, eventType, paymentId);
	}

	private <T> boolean post(T payload, String endpoint, HttpHeaders headers, String successMsg, String eventType,
			String paymentId) {
		try {
			this.logUtility.logMessage(successMsg, INFO, log);
			ResponseEntity<String> response = this.webhookClient.execute((client) -> client.post()
				.uri(endpoint)
				.headers((requestHeaders) -> requestHeaders.addAll(headers))
				.body(payload)
				.retrieve()
				.toEntity(String.class));

			if (response.getStatusCode().is2xxSuccessful()) {
				logSuccess(endpoint, eventType, response);
				return true;
			}
			logFailure(endpoint, response, eventType, paymentId);
			return false;
		}
		catch (Exception ex) {
			this.logUtility.logMessage("Webhook invocation failed" + ((eventType != null) ? " for " + eventType : "")
					+ ((paymentId != null) ? " and PaymentId: " + paymentId : "") + ": with error : " + ex.getMessage(),
					ERROR, log);
			return false;
		}
	}

	private void logSuccess(String endpoint, String eventType, ResponseEntity<String> response) {
		this.logUtility.logMessage(
				"Successfully invoked endpoint: " + endpoint + ((eventType != null) ? " for event: " + eventType : ""),
				DEBUG, log);
		this.logUtility.logMessage("Webhook Response : " + response, DEBUG, log);
	}

	private void logFailure(String endpoint, ResponseEntity<String> response, String eventType, String paymentId) {
		this.logUtility.logMessage("Webhook invocation failed for endpoint: " + endpoint + " with status code: "
				+ response.getStatusCode() + ((eventType != null) ? FOR + eventType : "")
				+ ((paymentId != null) ? " and PaymentId: " + paymentId : ""), ERROR, log);
	}

}
