package com.uhg.optum.financials.payments.payer.controller;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookBaseDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.LoadingWebhookRequestDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.text.StringEscapeUtils;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.DEBUG;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.INFO;

@RestController
@Slf4j
@RequestMapping("v2/webhook")
public class WebhookController {

	@Autowired
	private LogUtility logUtility;

	@Operation(summary = "Handle loading webhook notifications",
			description = " **NOTE:** Receives and logs loading webhook notifications from the VirtualCard Mock Service. This endpoint is typically used for testing and validating webhook payloads related to loading events.")
	@PostMapping("/loading")
	public ResponseEntity<String> handleLoadingNotifications(
			@RequestBody LoadingWebhookRequestDto loadingWebhookRequestDto) {
		this.logUtility.logMessage("Loading Response from virtual card Mock Service", DEBUG, log);
		ObjectMapper mapper = new ObjectMapper();
		mapper.registerModule(new JavaTimeModule());
		String response = null;
		try {
			response = mapper.writerWithDefaultPrettyPrinter().writeValueAsString(loadingWebhookRequestDto);
		}
		catch (JsonProcessingException ex) {
			throw new BadRequestException(ex.getMessage());
		}
		String sanitizedResponse = StringEscapeUtils.escapeHtml4(response);
		this.logUtility.logMessage("Loading Webhook Response: " + sanitizedResponse, INFO, log);
		return new ResponseEntity<>(sanitizedResponse, HttpStatus.OK);
	}

	@PostMapping("/event")
	public ResponseEntity<String> handleCardNotifications(
			@RequestBody CardEventWebhookBaseDto cardIssuedWebhookBaseDto) {
		this.logUtility.logMessage("card issued Response from virtual card Mock Service", DEBUG, log);
		ObjectMapper mapper = new ObjectMapper();
		mapper.registerModule(new JavaTimeModule());
		String response = null;
		try {
			response = mapper.writerWithDefaultPrettyPrinter().writeValueAsString(cardIssuedWebhookBaseDto);
		}
		catch (JsonProcessingException ex) {
			throw new BadRequestException(ex.getMessage());
		}
		String sanitizedResponse = StringEscapeUtils.escapeHtml4(response);
		this.logUtility.logMessage("card issued Webhook Response: " + sanitizedResponse, INFO, log);
		return new ResponseEntity<>(sanitizedResponse, HttpStatus.OK);
	}

	@Operation(summary = "Handle void webhook notifications",
			description = " **NOTE:** Receives and logs card void webhook notifications from the VirtualCard Mock Service. This endpoint is typically used for testing and validating webhook payloads related to void events.")
	@PostMapping("/voided")
	public ResponseEntity<String> handleVoidNotifications(
			@RequestBody LoadingWebhookRequestDto cardVoidWebhookRequestDto) {
		this.logUtility.logMessage("Card Void response from virtual card Mock Service", DEBUG, log);
		ObjectMapper mapper = new ObjectMapper();
		mapper.registerModule(new JavaTimeModule());
		String response = null;
		try {
			response = mapper.writerWithDefaultPrettyPrinter().writeValueAsString(cardVoidWebhookRequestDto);
		}
		catch (JsonProcessingException ex) {
			throw new BadRequestException(ex.getMessage());
		}
		String sanitizedResponse = StringEscapeUtils.escapeHtml4(response);
		this.logUtility.logMessage("Card Void Webhook Response: " + sanitizedResponse, INFO, log);
		return new ResponseEntity<>(sanitizedResponse, HttpStatus.OK);
	}

	@Operation(summary = "Handle card auth reversed webhook notifications",
			description = " **NOTE:** Receives and logs card auth reversed webhook notifications from the VirtualCard Mock Service. This endpoint is typically used for testing and validating webhook payloads related to card auth reversed events.")
	@PostMapping("/auth-reversed")
	public ResponseEntity<String> handleCardAuthReversedNotifications(
			@RequestBody CardEventWebhookBaseDto cardAuthReversedWebhookBaseDto) {
		this.logUtility.logMessage("Card Auth Reversed response from virtual card Mock Service", DEBUG, log);
		ObjectMapper mapper = new ObjectMapper();
		mapper.registerModule(new JavaTimeModule());
		String response = null;
		try {
			response = mapper.writerWithDefaultPrettyPrinter().writeValueAsString(cardAuthReversedWebhookBaseDto);
		}
		catch (JsonProcessingException ex) {
			throw new BadRequestException(ex.getMessage());
		}
		String sanitizedResponse = StringEscapeUtils.escapeHtml4(response);
		this.logUtility.logMessage("Card Auth Reversed Webhook Response: " + sanitizedResponse, INFO, log);
		return new ResponseEntity<>(sanitizedResponse, HttpStatus.OK);
	}

	@Operation(summary = "Handle card settled webhook notifications",
			description = " **NOTE:** Receives and logs settled webhook notifications from the VirtualCard Mock Service. This endpoint is typically used for testing and validating webhook payloads related to clear extract events.")
	@PostMapping("/settled")
	public ResponseEntity<String> handleSettledNotifications(
			@RequestBody LoadingWebhookRequestDto loadingWebhookRequestDto) {
		this.logUtility.logMessage("Loading Response from virtual card Mock Service", DEBUG, log);
		ObjectMapper mapper = new ObjectMapper();
		mapper.registerModule(new JavaTimeModule());
		String response = null;
		try {
			response = mapper.writerWithDefaultPrettyPrinter().writeValueAsString(loadingWebhookRequestDto);
		}
		catch (JsonProcessingException ex) {
			throw new BadRequestException(ex.getMessage());
		}
		String sanitizedResponse = StringEscapeUtils.escapeHtml4(response);
		this.logUtility.logMessage("Loading Webhook Response: " + sanitizedResponse, INFO, log);
		return new ResponseEntity<>(sanitizedResponse, HttpStatus.OK);
	}

}
