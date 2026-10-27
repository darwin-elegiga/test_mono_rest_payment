package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.CancelCardRequest;
import com.uhg.optum.financials.payments.payer.dto.CreateCardRequest;
import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@Slf4j
@Tag(name = "Payments")
@RestController
@RequestMapping("v1")
public class CreateVirtualCardMockController {

	@PostMapping("/create-requests")
	public ResponseEntity<String> createCards(@RequestBody @Valid CreateCardRequest createCardRequest) {
		log.info("Virtual card created successfully with HTTP status: {}", HttpStatus.ACCEPTED);
		return new ResponseEntity<>("Virtual card created successfully", HttpStatus.ACCEPTED);
	}

	@PostMapping("/{card-guid}/cancel-requests")
	public ResponseEntity<String> cancelCard(@RequestBody @Valid CancelCardRequest cancelCardRequest) {
		log.info("Virtual card cancelled successfully with HTTP status: {}", HttpStatus.ACCEPTED);
		return new ResponseEntity<>("Virtual card cancelled successfully", HttpStatus.ACCEPTED);
	}

	@PostMapping("/{cardGuid}/reset-requests")
	public ResponseEntity<String> resetCard(@RequestBody @Valid ResetCardRequest resetCardRequest) {
		log.info("Virtual card reset successfully with HTTP status: {}", HttpStatus.ACCEPTED);
		return new ResponseEntity<>("Virtual card reset successfully", HttpStatus.ACCEPTED);
	}

}
