package com.uhg.optum.financials.payments.payer.controller;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.dto.stp.StpVirtualCardRequestDto;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.MediaType;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.util.StringUtils;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.time.Instant;
import java.util.LinkedHashMap;
import java.util.Map;

import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.INFO;

@Tag(name = "STP Mock Remittance", description = "Mock endpoint for testing STP virtual card remittance submissions")
@RestController
@RequestMapping("mock/stp")
@Slf4j
public class StpRemittanceMockController {

	private final LogUtility logUtility;

	private final ObjectMapper objectMapper;

	public StpRemittanceMockController(LogUtility logUtility, ObjectMapper objectMapper) {
		this.logUtility = logUtility;
		this.objectMapper = objectMapper;
	}

	@Operation(summary = "No-auth STP Provider endpoint",
			description = "No-auth endpoint for STP Provider. Both C2B and B2B return success.")
	@PostMapping("/remittance/no-auth/stp-provider")
	public ResponseEntity<Map<String, Object>> receiveRemittanceNoAuthStpProvider(
			@RequestBody StpVirtualCardRequestDto payload) {
		logReceivedPayload("no-auth/stp-provider", payload);
		return accepted(payload);
	}

	@Operation(summary = "Auth STP Provider1 endpoint",
			description = "Auth endpoint for STP Provider1. Both C2B and B2B return success.")
	@PostMapping("/remittance/auth/stp-provider1")
	public ResponseEntity<Map<String, Object>> receiveRemittanceAuthStpProvider1(
			@RequestBody StpVirtualCardRequestDto payload,
			@RequestHeader(name = "Authorization", required = false) String authorization) {
		logReceivedPayload("auth/stp-provider1", payload);
		if (!isValidBearerToken(authorization)) {
			return ResponseEntity.status(HttpStatus.UNAUTHORIZED)
				.body(errorResponse("UNAUTHORIZED", "Missing or invalid bearer token",
						extractNetworkPaymentId(payload)));
		}
		return accepted(payload);
	}

	@Operation(summary = "Auth STP Provider2 endpoint",
			description = "Auth endpoint for STP Provider2. C2B returns 400, B2B throws runtime exception.")
	@PostMapping("/remittance/auth/stp-provider2")
	public ResponseEntity<Map<String, Object>> receiveRemittanceAuthStpProvider2(
			@RequestBody StpVirtualCardRequestDto payload,
			@RequestHeader(name = "Authorization", required = false) String authorization) {
		logReceivedPayload("auth/stp-provider2", payload);
		if (!isValidBearerToken(authorization)) {
			return ResponseEntity.status(HttpStatus.UNAUTHORIZED)
				.body(errorResponse("UNAUTHORIZED", "Missing or invalid bearer token",
						extractNetworkPaymentId(payload)));
		}

		if (isC2b(payload)) {
			return ResponseEntity.status(HttpStatus.BAD_REQUEST)
				.body(errorResponse("BUSINESS_REJECTION", "C2B simulated 4xx for auth STP Provider2",
						extractNetworkPaymentId(payload)));
		}

		throw new IllegalStateException("B2B simulated runtime exception for auth STP Provider2");
	}

	@Operation(summary = "Mock OAuth token endpoint",
			description = "Returns a static bearer token for STP remittance mock testing")
	@PostMapping(value = "/oauth/token", consumes = MediaType.APPLICATION_FORM_URLENCODED_VALUE)
	public ResponseEntity<Map<String, Object>> token(
			@RequestParam(name = "grant_type", required = false) String grantType,
			@RequestParam(name = "scope", required = false) String scope) {
		Map<String, Object> response = new LinkedHashMap<>();
		response.put("access_token", "mock-stp-token");
		response.put("token_type", "Bearer");
		response.put("expires_in", 3600);
		response.put("scope", StringUtils.hasText(scope) ? scope : "stp.scope");
		response.put("grant_type", StringUtils.hasText(grantType) ? grantType : "client_credentials");
		response.put("issued_at", Instant.now().toString());
		this.logUtility.logMessage("Issued mock STP OAuth token", INFO, log);
		return ResponseEntity.ok(response);
	}

	private ResponseEntity<Map<String, Object>> accepted(StpVirtualCardRequestDto payload) {
		String networkPaymentId = extractNetworkPaymentId(payload);
		String networkPartner = extractNetworkPartner(payload);
		String safeNetworkPaymentId = sanitizeForLog(networkPaymentId);
		String safeNetworkPartner = sanitizeForLog(networkPartner);

		this.logUtility
			.logMessage(String.format("[networkPaymentId=%s] [networkPartner=%s] Received mock STP remittance payload",
					safeNetworkPaymentId, safeNetworkPartner), INFO, log);

		Map<String, Object> response = new LinkedHashMap<>();
		response.put("status", "ACCEPTED");
		response.put("networkPaymentId", networkPaymentId);
		response.put("networkPartner", networkPartner);
		response.put("receivedAt", Instant.now().toString());
		response.put("message", "STP remittance received and accepted");

		log.info("[networkPaymentId={}] Mock STP remittance accepted", safeNetworkPaymentId);
		return ResponseEntity.status(HttpStatus.OK).body(response);
	}

	private void logReceivedPayload(String endpoint, StpVirtualCardRequestDto payload) {
		String safeEndpoint = sanitizeForLog(endpoint);
		String safeNetworkPaymentId = sanitizeForLog(extractNetworkPaymentId(payload));
		this.logUtility.logMessage(
				String.format("[endpoint=%s] [networkPaymentId=%s] Received mock STP remittance request payload=%s",
						safeEndpoint, safeNetworkPaymentId, serializePayloadForLog(payload)),
				INFO, log);
	}

	private String serializePayloadForLog(StpVirtualCardRequestDto payload) {
		if (payload == null) {
			return "null";
		}
		try {
			return sanitizeForLog(this.objectMapper.writeValueAsString(payload));
		}
		catch (JsonProcessingException ex) {
			return "<unserializable-payload>";
		}
	}

	private String sanitizeForLog(String value) {
		if (value == null) {
			return "null";
		}
		return value.replace('\n', '_').replace('\r', '_');
	}

	private boolean isC2b(StpVirtualCardRequestDto payload) {
		return payload != null && payload.getConsumer() != null;
	}

	private boolean isValidBearerToken(String authorization) {
		if (!StringUtils.hasText(authorization)) {
			return false;
		}
		if (!authorization.startsWith("Bearer ")) {
			return false;
		}
		String token = authorization.substring("Bearer ".length()).trim();
		return "mock-stp-token".equals(token);
	}

	private String extractNetworkPaymentId(StpVirtualCardRequestDto payload) {
		if (payload == null || payload.getHeader() == null) {
			return "UNKNOWN";
		}
		String id = payload.getHeader().getNetworkPaymentId();
		return StringUtils.hasText(id) ? id : "UNKNOWN";
	}

	private String extractNetworkPartner(StpVirtualCardRequestDto payload) {
		if (payload == null || payload.getHeader() == null) {
			return "UNKNOWN";
		}
		String partner = payload.getHeader().getNetworkPartner();
		return StringUtils.hasText(partner) ? partner : "UNKNOWN";
	}

	private Map<String, Object> errorResponse(String code, String message, String networkPaymentId) {
		Map<String, Object> error = new LinkedHashMap<>();
		error.put("status", "ERROR");
		error.put("code", code);
		error.put("message", message);
		error.put("networkPaymentId", networkPaymentId);
		error.put("timestamp", Instant.now().toString());
		return error;
	}

}
