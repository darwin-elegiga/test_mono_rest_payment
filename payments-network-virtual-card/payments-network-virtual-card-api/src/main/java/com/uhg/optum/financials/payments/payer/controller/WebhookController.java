package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.authevent.AuthorizationPayload;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.service.ProcessFailedEventService;
import com.uhg.optum.financials.payments.payer.service.ZetaWebhookService;

import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.SUCCESS;

@Slf4j
@Tag(name = "Virtual Card Callbacks", description = "Virtual Card Callbacks API")
@RequestMapping("cards")
@RestController
public class WebhookController {

	private final ZetaWebhookService zetaWebhookService;

	private final ProcessFailedEventService processFailedEventService;

	public WebhookController(ZetaWebhookService zetaWebhookService,
			ProcessFailedEventService processFailedEventService) {
		this.zetaWebhookService = zetaWebhookService;
		this.processFailedEventService = processFailedEventService;
	}

	@PreAuthorize("hasAuthority('org:networks:write')")
	@PostMapping("/webhooks")
	public ResponseEntity<?> processZetaCallbackResponse(@RequestBody @Valid ZetaWebhookDto<?> zetaWebhookDto) {
		try {
			int inserted = this.zetaWebhookService.processZetaWebhook(zetaWebhookDto);
			if (inserted == 0) {
				return new ResponseEntity<>("Duplicate webhook received", HttpStatus.CREATED);
			}
			return new ResponseEntity<>(SUCCESS, HttpStatus.OK);
		}
		catch (IllegalStateException ex) {
			return new ResponseEntity<>("Internal server error: " + ex.getMessage(), HttpStatus.INTERNAL_SERVER_ERROR);
		}
	}

	@PostMapping("/auth-events")
	public ResponseEntity<?> processAuthEvents(@RequestBody @Valid AuthorizationPayload authorizationPayload) {
		return new ResponseEntity<>(SUCCESS, HttpStatus.OK);
	}

	@PreAuthorize("hasAuthority('org:networks:write')")
	@PostMapping("/failed-events")
	public ResponseEntity<?> processFailedEvents(@RequestParam(required = false) String paymentId,
			@RequestParam(required = false) String correlationId) {

		if ((paymentId == null || paymentId.isBlank()) && (correlationId == null || correlationId.isBlank())) {
			throw new BadRequestException("Either paymentId or correlationId must be provided");
		}

		if ((paymentId != null && !paymentId.isBlank()) && (correlationId != null && !correlationId.isBlank())) {

			throw new BadRequestException("Only one of paymentId or correlationId should be provided, not both");
		}

		this.processFailedEventService.processFailedEvent(paymentId, correlationId);
		return new ResponseEntity<>(SUCCESS, HttpStatus.OK);
	}

}
