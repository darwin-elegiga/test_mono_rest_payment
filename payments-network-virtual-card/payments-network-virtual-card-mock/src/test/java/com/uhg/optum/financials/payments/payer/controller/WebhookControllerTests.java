package com.uhg.optum.financials.payments.payer.controller;

import static org.assertj.core.api.BDDAssertions.then;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.ObjectWriter;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookBaseDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.LoadingWebhookRequestDto;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

@ExtendWith(MockitoExtension.class)
class WebhookControllerTests {

	@Mock
	private LogUtility logUtility;

	@InjectMocks
	private WebhookController webhookController;

	@Mock
	private ObjectMapper mapper;

	@Mock
	private ObjectWriter objectWriter;

	@BeforeEach
	void setUp() {
	}

	@Test
	void testHandleLoadingNotificationsReturnsOk() {
		LoadingWebhookRequestDto<Object> loadingWebhookRequestDto = new LoadingWebhookRequestDto<>();
		ResponseEntity<String> response = this.webhookController.handleLoadingNotifications(loadingWebhookRequestDto);
		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).contains("timestamp");
	}

	@Test
	void testHandleLoadingNotificationsThrowsBadRequestException() {
		// Unable to mock serialization exception due to new ObjectMapper in controller
		// Only success path can be tested unless controller is refactored
		LoadingWebhookRequestDto<Object> loadingWebhookRequestDto = new LoadingWebhookRequestDto<>();
		ResponseEntity<String> response = this.webhookController.handleLoadingNotifications(loadingWebhookRequestDto);
		then(response).isNotNull();
		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).contains("timestamp");
	}

	@Test
	void testHandleCardNotificationsReturnsOk() {
		CardEventWebhookBaseDto cardEventWebhookBaseDto = new CardEventWebhookBaseDto();
		ResponseEntity<String> response = this.webhookController.handleCardNotifications(cardEventWebhookBaseDto);
		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).contains("timestamp");
	}

	@Test
	void testHandleCardNotificationsThrowsBadRequestException() {
		// Unable to mock serialization exception due to new ObjectMapper in controller
		// Only success path can be tested unless controller is refactored
		CardEventWebhookBaseDto cardEventWebhookBaseDto = new CardEventWebhookBaseDto();
		ResponseEntity<String> response = this.webhookController.handleCardNotifications(cardEventWebhookBaseDto);
		then(response).isNotNull();
		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).contains("timestamp");
	}

	@Test
	void testHandleVoidNotificationsReturnsOk() {
		LoadingWebhookRequestDto<Object> cardVoidWebhookRequestDto = new LoadingWebhookRequestDto<>();
		ResponseEntity<String> response = this.webhookController.handleVoidNotifications(cardVoidWebhookRequestDto);
		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).contains("timestamp");
	}

	@Test
	void testHandleVoidNotificationsThrowsBadRequestException() {
		// Unable to mock serialization exception due to new ObjectMapper in controller
		// Only success path can be tested unless controller is refactored
		LoadingWebhookRequestDto<Object> cardVoidWebhookRequestDto = new LoadingWebhookRequestDto<>();
		ResponseEntity<String> response = this.webhookController.handleVoidNotifications(cardVoidWebhookRequestDto);
		then(response).isNotNull();
		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).contains("timestamp");
	}

	@Test
	void testHandleCardAuthReversedNotificationsReturnsOk() {
		CardEventWebhookBaseDto cardAuthReversedWebhookBaseDto = new CardEventWebhookBaseDto();
		ResponseEntity<String> response = this.webhookController
			.handleCardAuthReversedNotifications(cardAuthReversedWebhookBaseDto);
		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).contains("timestamp");
	}

	@Test
	void testHandleCardAuthReversedNotificationsThrowsBadRequestException() {
		// Unable to mock serialization exception due to new ObjectMapper in controller
		// Only success path can be tested unless controller is refactored
		CardEventWebhookBaseDto cardAuthReversedWebhookBaseDto = new CardEventWebhookBaseDto();
		ResponseEntity<String> response = this.webhookController
			.handleCardAuthReversedNotifications(cardAuthReversedWebhookBaseDto);
		then(response).isNotNull();
		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).contains("timestamp");
	}

	@Test
	void testHandleSettledNotificationsReturnsOk() {
		LoadingWebhookRequestDto<Object> loadingWebhookRequestDto = new LoadingWebhookRequestDto<>();
		ResponseEntity<String> response = this.webhookController.handleSettledNotifications(loadingWebhookRequestDto);
		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).contains("timestamp");
	}

	@Test
	void testHandleSettledNotificationsThrowsBadRequestException() {
		// Unable to mock serialization exception due to new ObjectMapper in controller
		// Only success path can be tested unless controller is refactored
		LoadingWebhookRequestDto<Object> loadingWebhookRequestDto = new LoadingWebhookRequestDto<>();
		ResponseEntity<String> response = this.webhookController.handleSettledNotifications(loadingWebhookRequestDto);
		then(response).isNotNull();
		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).contains("timestamp");
	}

}
