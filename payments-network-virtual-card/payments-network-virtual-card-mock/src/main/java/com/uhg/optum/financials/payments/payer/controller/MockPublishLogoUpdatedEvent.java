package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.LogoDetailsRequest;
import com.uhg.optum.financials.payments.payer.dto.ResponseDto;
import com.uhg.optum.financials.payments.payer.dto.logo.LogoEventDto;
import com.uhg.optum.financials.payments.payer.model.LogoDetails;
import com.uhg.optum.financials.payments.payer.service.LogoUpdatedEventService;
import io.swagger.v3.oas.annotations.Operation;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/**
 * Description: MockPublishLogoUpdatedEvent.
 *
 * @author bhemabh, created on 12-August-2024
 */
@RestController
@Slf4j
@RequestMapping("v1")
public class MockPublishLogoUpdatedEvent {

	@Autowired
	private LogoUpdatedEventService logoUpdatedEventService;

	@Operation(summary = "Send logo updated event to event bus",
			description = " **NOTE:** This endpoint receives a logo update event and publishes it to the event bus. It is typically used when a org or account and sub-account logo is updated, ensuring that services are notified of the change.")
	@PostMapping("/accounts/produce/logo-updated-event")
	public ResponseEntity<ResponseDto<String>> sendLogoUpdatedEvent(@RequestBody LogoEventDto logoEventDto) {
		log.info("Sending logo event to event bus");
		return ResponseEntity.ok(new ResponseDto<>(this.logoUpdatedEventService.publishLogoUpdatedEvent(logoEventDto),
				true, logoEventDto.getId().toString()));

	}

	@Operation(summary = "Fetch logo details for an account",
			description = " **NOTE:** This endpoint retrieves logo details for a given account, sub-account, and organization short code.")
	@PostMapping("/logo-details")
	public ResponseEntity<?> getLogoDetails(@RequestBody LogoDetailsRequest request) {
		log.info("Getting logo details");
		try {
			LogoDetails logoDetails = this.logoUpdatedEventService.getLogoDetails(request.getAccountShortCode(),
					request.getSubAccountShortCode(), request.getOrgShortCode());
			return new ResponseEntity<>(logoDetails, HttpStatus.OK);
		}
		catch (Exception ex) {
			log.error("Exception occurred while fetching logo details request", ex);
			return new ResponseEntity<>("Exception occurred while getting logo details", HttpStatus.NOT_FOUND);
		}
	}

}
