package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.webhook.WebhookBaseDto;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.test.util.ReflectionTestUtils;

import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
public class WebhookClientImplTests {

	private WebhookClientImpl webhookClientImpl;

	@Mock
	private ResilientRestClientFactory resilientRestClientFactory;

	@Mock
	private ResilientRestClient resilientRestClient;

	@Mock
	private LogUtility logUtility;

	@BeforeEach
	void setUp() {
		Mockito.when(this.resilientRestClientFactory.getClient("webhook")).thenReturn(this.resilientRestClient);
		this.webhookClientImpl = new WebhookClientImpl(this.resilientRestClientFactory, this.logUtility);
	}

	@Test
	void testNotifyClient_SuccessfulFirstAttempt() {
		// Arrange
		WebhookBaseDto webhookBaseDto = Mockito.mock(WebhookBaseDto.class);
		Mockito.when(webhookBaseDto.getType()).thenReturn("payment.created");

		ResponseEntity<String> successResponse = ResponseEntity.ok("Success");
		Mockito.when(this.resilientRestClient.execute(any())).thenReturn(successResponse);

		// Act
		this.webhookClientImpl.notifyClient(webhookBaseDto, "endpoint");

		// Assert
		Mockito.verify(this.resilientRestClient, Mockito.times(1)).execute(any());
	}

	@Test
	void testNotifyClient_WithNonSuccessResponse() {
		// Arrange - ResilientRestClient handles retries transparently
		WebhookBaseDto webhookBaseDto = Mockito.mock(WebhookBaseDto.class);
		Mockito.when(webhookBaseDto.getType()).thenReturn("payment.created");

		ResponseEntity<String> serverErrorResponse = ResponseEntity.status(500).body("Server Error");
		Mockito.when(this.resilientRestClient.execute(any())).thenReturn(serverErrorResponse);

		// Act
		this.webhookClientImpl.notifyClient(webhookBaseDto, "endpoint");

		// Assert - Single call since retries are handled by ResilientRestClient
		Mockito.verify(this.resilientRestClient, Mockito.times(1)).execute(any());
		Mockito.verify(this.logUtility)
			.logMessage(Mockito.contains("Webhook invocation failed for endpoint"), Mockito.eq("error"), Mockito.any());
	}

	@Test
	void testLogSuccess() {
		ResponseEntity<String> response = ResponseEntity.ok("Success");
		ReflectionTestUtils.invokeMethod(this.webhookClientImpl, "logSuccess", "endpoint", "eventType", response);
		Mockito.verify(this.logUtility, Mockito.atLeastOnce())
			.logMessage(Mockito.contains("Successfully invoked endpoint: endpoint for event: eventType"),
					Mockito.eq("debug"), Mockito.any());
		Mockito.verify(this.logUtility, Mockito.atLeastOnce())
			.logMessage(Mockito.contains("Webhook Response"), Mockito.eq("debug"), Mockito.any());
	}

	@Test
	void testLogFailure() {
		ResponseEntity<String> response = ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body("fail");
		ReflectionTestUtils.invokeMethod(this.webhookClientImpl, "logFailure", "endpoint", response, "eventType",
				"paymentId");
		Mockito.verify(this.logUtility, Mockito.atLeastOnce())
			.logMessage(Mockito.contains(
					"Webhook invocation failed for endpoint: endpoint with status code: 500 INTERNAL_SERVER_ERRORfor:eventType and PaymentId: paymentId"),
					Mockito.eq("error"), Mockito.any());
	}

}
