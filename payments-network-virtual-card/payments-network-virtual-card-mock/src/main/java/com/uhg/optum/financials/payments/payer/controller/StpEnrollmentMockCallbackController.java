package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentCallbackDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentCallbackEnrollmentErrorDto;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.SUCCESS;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.INFO;

@Tag(name = "STP Mock Callback", description = "Mock endpoint for testing STP enrollment validation callbacks")
@RestController
@RequestMapping("cards")
public class StpEnrollmentMockCallbackController {

	private final LogUtility logUtility;

	public StpEnrollmentMockCallbackController(LogUtility logUtility) {
		this.logUtility = logUtility;
	}

	@Operation(summary = "Receives mock STP enrollment validation callback payloads for local testing")
	@PostMapping("/stp-enrollment-callback")
	public ResponseEntity<Map<String, Object>> receiveMockCallback(@RequestBody StpEnrollmentCallbackDto callbackDto) {
		List<StpEnrollmentCallbackEnrollmentErrorDto> enrollmentErrors = (callbackDto != null
				&& callbackDto.getEnrollmentErrors() != null) ? callbackDto.getEnrollmentErrors() : List.of();
		int errorCount = enrollmentErrors.stream()
			.mapToInt((error) -> (error.getErrors() != null) ? error.getErrors().size() : 0)
			.sum();
		String correlationId = (callbackDto != null && callbackDto.getHeader() != null)
				? callbackDto.getHeader().getCorrelationId() : null;

		this.logUtility.logMessage(
				String.format("[correlationId=%s] Received mock STP enrollment callback with %d validation errors",
						correlationId, errorCount),
				INFO, org.slf4j.LoggerFactory.getLogger(StpEnrollmentMockCallbackController.class));

		Map<String, Object> response = new LinkedHashMap<>();
		response.put("status", SUCCESS);
		response.put("receivedErrors", errorCount);
		response.put("correlationId", correlationId);
		response.put("errorSummary", buildErrorSummary(enrollmentErrors));
		return ResponseEntity.status(HttpStatus.OK).body(response);
	}

	private List<Map<String, Object>> buildErrorSummary(
			List<StpEnrollmentCallbackEnrollmentErrorDto> enrollmentErrors) {
		return enrollmentErrors.stream().map((enrollmentError) -> {
			Map<String, Object> summary = new LinkedHashMap<>();
			summary.put("providerTin", enrollmentError.getProviderTin());
			summary.put("npi", enrollmentError.getNpi());
			summary.put("enrollmentType", enrollmentError.getEnrollmentType());
			summary.put("errorCount", (enrollmentError.getErrors() != null) ? enrollmentError.getErrors().size() : 0);
			summary.put("errors", enrollmentError.getErrors());
			return summary;
		}).toList();
	}

}
