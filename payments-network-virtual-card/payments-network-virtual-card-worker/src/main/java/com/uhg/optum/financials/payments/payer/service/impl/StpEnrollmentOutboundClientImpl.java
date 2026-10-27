package com.uhg.optum.financials.payments.payer.service.impl;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentCallbackDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpProviderEnrollmentRequestDto;
import com.uhg.optum.financials.payments.payer.service.StpEnrollmentOutboundClient;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.core.env.Environment;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;

@Service
@Slf4j
public class StpEnrollmentOutboundClientImpl implements StpEnrollmentOutboundClient {

	private final ResilientRestClient callbackClient;

	private final ResilientRestClient tokenClient;

	private final ObjectMapper objectMapper;

	private final LogUtility logUtility;

	private final Environment environment;

	private final SecretService secretService;

	public StpEnrollmentOutboundClientImpl(ResilientRestClientFactory resilientRestClientFactory,
			ObjectMapper objectMapper, LogUtility logUtility, Environment environment, SecretService secretService) {
		this.callbackClient = resilientRestClientFactory.getClient("stp-enrollment");
		this.tokenClient = resilientRestClientFactory.getClient("stp-enrollment-token");
		this.objectMapper = objectMapper;
		this.logUtility = logUtility;
		this.environment = environment;
		this.secretService = secretService;
	}

	@Override
	public boolean sendValidationCallback(StpEnrollmentCallbackDto callbackDto, String endpoint) {
		HttpHeaders headers = new HttpHeaders();
		headers.setContentType(MediaType.APPLICATION_JSON);

		// Fetch and apply OAuth Bearer token if configured for this provider
		String networkPartner = (callbackDto != null && callbackDto.getHeader() != null)
				? callbackDto.getHeader().getNetworkPartner() : null;
		if (StringUtils.hasText(networkPartner)) {
			applyOAuthForEnrollment(networkPartner.toLowerCase(), headers);
		}

		return postJson(callbackDto, endpoint, "STP enrollment validation callback", headers);
	}

	@Override
	public boolean sendToPreferencing(StpProviderEnrollmentRequestDto requestDto, String endpoint) {
		HttpHeaders headers = new HttpHeaders();
		headers.setContentType(MediaType.APPLICATION_JSON);
		return postJson(requestDto, endpoint, "STP enrollment preferencing handoff", headers);
	}

	/**
	 * Applies OAuth Bearer token to headers. Reads auth-type and scope from the
	 * enrollment prefix. TokenUrl and client credentials are resolved from the common
	 * {@link StpOAuthHelper#CREDENTIAL_PREFIX} shared with remittance.
	 */
	private void applyOAuthForEnrollment(String providerKey, HttpHeaders headers) {
		String enrollmentPrefix = "stp.enrollment.providers." + providerKey + ".";
		String authType = this.environment.getProperty(enrollmentPrefix + "auth-type");
		if (!StpOAuthHelper.isOAuthType(authType)) {
			return;
		}
		String tokenUrl = StpOAuthHelper.resolveTokenUrl(providerKey, this.environment);
		String scope = this.environment.getProperty(enrollmentPrefix + "oauth.scope");
		String[] credentials = StpOAuthHelper.resolveOAuthCredentials(providerKey, this.secretService);
		String token = StpOAuthHelper.fetchAccessToken(providerKey, this.tokenClient, this.objectMapper, tokenUrl,
				credentials[0], credentials[1], scope);
		if (StringUtils.hasText(token)) {
			headers.setBearerAuth(token);
		}
	}

	private boolean postJson(Object payload, String endpoint, String description, HttpHeaders headers) {
		if (!StringUtils.hasText(endpoint)) {
			this.logUtility.logMessage(description + " skipped because endpoint is not configured", INFO, log);
			return false;
		}
		try {
			this.logUtility.logMessage(
					description + " request body for endpoint: " + endpoint + " payload: " + formatPayload(payload),
					INFO, log);
			ResponseEntity<String> response = this.callbackClient.execute((client) -> client.post()
				.uri(endpoint)
				.headers((requestHeaders) -> requestHeaders.addAll(headers))
				.body(payload)
				.retrieve()
				.toEntity(String.class));
			boolean success = response.getStatusCode().is2xxSuccessful();
			if (success) {
				this.logUtility.logMessage(description + " succeeded for endpoint: " + endpoint + " with status: "
						+ response.getStatusCode(), INFO, log);
			}
			else {
				this.logUtility.logMessage(
						description + " failed for endpoint: " + endpoint + " with status: " + response.getStatusCode(),
						ERROR, log);
			}
			return success;
		}
		catch (Exception ex) {
			this.logUtility.logMessage(
					description + " failed for endpoint: " + endpoint + " with error: " + ex.getMessage(), ERROR, log);
			return false;
		}
	}

	private String formatPayload(Object payload) {
		if (payload == null) {
			return "<empty>";
		}
		try {
			return this.objectMapper.writeValueAsString(payload);
		}
		catch (JsonProcessingException ex) {
			return "<unserializable payload>";
		}
	}

}
