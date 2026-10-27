package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.CustomRemarkUpdatedEventDto;
import com.uhg.optum.financials.payments.payer.dto.ResponseDto;
import com.uhg.optum.financials.payments.payer.model.DocumentCustomRemarks;
import com.uhg.optum.financials.payments.payer.repository.DocumentCustomRemarksRepository;
import com.uhg.optum.financials.payments.payer.service.CustomRemarksPublisherService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Lazy;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;

@RestController
@Slf4j
@RequestMapping("v1")
public class MockPublishCustomRemarksEvent {

	@Autowired
	private CustomRemarksPublisherService customRemarksPublisherService;

	@Autowired
	LogUtility logUtility;

	@Autowired
	DocumentCustomRemarksRepository documentCustomRemarksRepository;

	@Operation(summary = "Send customRemark updated event to event bus",
			description = " **NOTE:** This endpoint receives a logo update event and publishes it to the event bus. It is typically used when a org or account and sub-account logo is updated, ensuring that services are notified of the change.")
	@PostMapping("/produce/custom-remark-updated-event")
	public ResponseEntity<ResponseDto<String>> sendCustomRemarksUpdatedEvent(
			@RequestBody CustomRemarkUpdatedEventDto customRemarkUpdatedEventDto) {
		log.info("Sending logo event to event bus");
		return ResponseEntity.ok(new ResponseDto<>(
				this.customRemarksPublisherService.publishCustomRemarksUpdatedEvent(customRemarkUpdatedEventDto), true,
				customRemarkUpdatedEventDto.getId().toString()));

	}

	@Operation(summary = "Get custom remark Details",
			description = " **NOTE:** Retrieves detailed information about a registered sub-account using the provided organization short code, account short code, and sub-account short code.")
	@GetMapping("/custom-remarks/{orgShortCode}/{accountShortCode}/{subAccountShortCode}")
	@Lazy
	public ResponseEntity<DocumentCustomRemarks> getCustomRemarks(@PathVariable String orgShortCode,
			@PathVariable String accountShortCode, @PathVariable String subAccountShortCode) {
		this.logUtility.logMessage("Getting custom Remark Details", INFO, log);
		return ResponseEntity
			.ok(this.documentCustomRemarksRepository.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode(
					orgShortCode, accountShortCode, subAccountShortCode));
	}

}
