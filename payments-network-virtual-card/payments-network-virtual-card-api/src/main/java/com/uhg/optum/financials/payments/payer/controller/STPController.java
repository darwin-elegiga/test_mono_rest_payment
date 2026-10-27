package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.stp.StpProviderEnrollmentRequestDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpProviderEnrollmentResponseDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpProviderEnrollmentResponseHeaderDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpRejectReasonDto;
import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.producer.StorageQueueProducerApi;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.FAILED;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;

@RestController
@Slf4j
@Tag(name = "STP Provider Enrollment")
@RequestMapping("/partner-providers")
public class STPController {

	private static final String ACK_STATUS = "Success";

	@Autowired
	private StorageQueueProducerApi storageQueueProducerApi;

	@Autowired
	private LogUtility logUtility;

	@Value("${stp.enrollment.callback-url:}")
	private String callbackUrl;

	@Operation(summary = "Receive provider enrollment batch from STP network partner",
			description = " **NOTE**: Accepts a batch provider enrollment request from an STP network partner, "
					+ "queues it for asynchronous processing, and immediately acknowledges receipt.")
	@PostMapping("/enrollments")
	public ResponseEntity<StpProviderEnrollmentResponseDto> receiveProviderEnrollment(
			@RequestBody StpProviderEnrollmentRequestDto enrollmentRequest) {
		String correlationId = getCorrelationId(enrollmentRequest);
		String networkPartner = getNetworkPartner(enrollmentRequest);
		int providerCount = getProviderCount(enrollmentRequest);
		int enrollmentCount = getEnrollmentCount(enrollmentRequest);

		this.logUtility.logMessage(String.format(
				"[correlationId=%s] [networkPartner=%s] STP enrollment request received with %d providers and %d enrollment records",
				correlationId, networkPartner, providerCount, enrollmentCount), INFO, log);

		String enqueueStatus = this.storageQueueProducerApi.sendMessageToQueue(enrollmentRequest);
		if (FAILED.equalsIgnoreCase(enqueueStatus)) {
			throw new InternalServerException("Failed to enqueue STP provider enrollment request");
		}

		this.logUtility.logMessage(String.format(
				"[correlationId=%s] [networkPartner=%s] STP enrollment request queued for asynchronous processing",
				correlationId, networkPartner), INFO, log);

		return ResponseEntity.status(HttpStatus.ACCEPTED).body(buildAcknowledgement(enrollmentRequest));
	}

	@Operation(summary = "STP Payment Reject",
			description = " **NOTE**: STP Network Partner is unable to process a remittance ")
	@PostMapping("/payment/reject")
	public ResponseEntity<String> stpPaymentRejectReason(@Valid @RequestBody StpRejectReasonDto stpRejectReasonDto) {
		this.logUtility.logMessage("Fetching STP payment for networkId: " + stpRejectReasonDto.getOptumTransactionId(),
				INFO, log);

		String enqueueStatus = this.storageQueueProducerApi.sendMessageToQueue(stpRejectReasonDto);
		if (FAILED.equalsIgnoreCase(enqueueStatus)) {
			throw new InternalServerException("Failed to enqueue STP payment rejection request");
		}

		return ResponseEntity.status(HttpStatus.ACCEPTED).body(ACK_STATUS);
	}

	private StpProviderEnrollmentResponseDto buildAcknowledgement(StpProviderEnrollmentRequestDto enrollmentRequest) {
		StpProviderEnrollmentResponseHeaderDto header = new StpProviderEnrollmentResponseHeaderDto();
		header.setCallbackUrl(this.callbackUrl);
		header.setCorrelationId(getCorrelationId(enrollmentRequest));
		header.setNetworkPartner(getNetworkPartner(enrollmentRequest));
		header.setStatus(ACK_STATUS);

		StpProviderEnrollmentResponseDto response = new StpProviderEnrollmentResponseDto();
		response.setHeader(header);
		return response;
	}

	private String getCorrelationId(StpProviderEnrollmentRequestDto enrollmentRequest) {
		return (enrollmentRequest != null && enrollmentRequest.getHeader() != null)
				? enrollmentRequest.getHeader().getCorrelationId() : null;
	}

	private String getNetworkPartner(StpProviderEnrollmentRequestDto enrollmentRequest) {
		return (enrollmentRequest != null && enrollmentRequest.getHeader() != null)
				? enrollmentRequest.getHeader().getNetworkPartner() : null;
	}

	private int getProviderCount(StpProviderEnrollmentRequestDto enrollmentRequest) {
		List<?> partnerProviders = (enrollmentRequest != null) ? enrollmentRequest.getPartnerProviders() : null;
		return (partnerProviders != null) ? partnerProviders.size() : 0;
	}

	private int getEnrollmentCount(StpProviderEnrollmentRequestDto enrollmentRequest) {
		if (enrollmentRequest == null || enrollmentRequest.getPartnerProviders() == null) {
			return 0;
		}
		return enrollmentRequest.getPartnerProviders()
			.stream()
			.mapToInt((provider) -> (provider.getEnrollmentList() != null) ? provider.getEnrollmentList().size() : 0)
			.sum();
	}

}
