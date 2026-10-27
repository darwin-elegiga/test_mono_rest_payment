package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.ResponseDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchEventDto;
import com.uhg.optum.financials.payments.payer.service.BatchEventPublishService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;

@RestController
@Slf4j
@RequestMapping("/v1")
public class MockPublishBatchSentEvent {

	private final BatchEventPublishService batchEventPublishService;

	private final LogUtility logUtility;

	public MockPublishBatchSentEvent(BatchEventPublishService batchEventPublishService, LogUtility logUtility) {
		this.batchEventPublishService = batchEventPublishService;
		this.logUtility = logUtility;
	}

	@Operation(summary = "Publish a batch event to the EventBus",
			description = " **NOTE:** Accepts a batch event payload and publishes it to the EventBus. This endpoint is typically used for sending event data related to batch event.")

	@PostMapping("/produce/batch/event")
	public ResponseEntity<ResponseDto<String>> publishBatchEvent(@RequestBody BatchEventDto batchEventDto) {
		this.logUtility.logMessage("Sending batch event message to event bus", INFO, log);
		String org = (batchEventDto.getCorrelationDto() != null) ? batchEventDto.getCorrelationDto().getOrg() : null;
		return ResponseEntity
			.ok(new ResponseDto<>(this.batchEventPublishService.publishBatchEvent(batchEventDto), true, org));
	}

}
