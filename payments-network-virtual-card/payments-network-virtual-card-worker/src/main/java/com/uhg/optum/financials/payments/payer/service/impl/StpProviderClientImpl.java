package com.uhg.optum.financials.payments.payer.service.impl;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.dto.stp.StpVirtualCardRequestDto;
import com.uhg.optum.financials.payments.payer.enums.StpPaymentProcessingStatus;
import com.uhg.optum.financials.payments.payer.model.StpPaymentProcessing;
import com.uhg.optum.financials.payments.payer.model.StpPaymentProcessingHistory;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.StpPaymentProcessingHistoryRepository;
import com.uhg.optum.financials.payments.payer.repository.StpPaymentProcessingRepository;
import com.uhg.optum.financials.payments.payer.service.StpProviderClient;
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
import org.springframework.web.client.HttpStatusCodeException;
import org.springframework.web.client.ResourceAccessException;

import java.util.Optional;

@Slf4j
@Service
public class StpProviderClientImpl implements StpProviderClient {

	private final ResilientRestClient remittanceClient;

	private final ResilientRestClient tokenClient;

	private final ObjectMapper objectMapper;

	private final StpPaymentProcessingRepository stpPaymentProcessingRepository;

	private final StpPaymentProcessingHistoryRepository stpPaymentProcessingHistoryRepository;

	private final PaymentRequestRepository paymentRequestRepository;

	private final SecretService secretService;

	private final Environment environment;

	public StpProviderClientImpl(ResilientRestClientFactory resilientRestClientFactory, ObjectMapper objectMapper,
			StpPaymentProcessingRepository stpPaymentProcessingRepository,
			StpPaymentProcessingHistoryRepository stpPaymentProcessingHistoryRepository,
			PaymentRequestRepository paymentRequestRepository, SecretService secretService, Environment environment) {
		this.remittanceClient = resilientRestClientFactory.getClient("stp-remittance");
		this.tokenClient = resilientRestClientFactory.getClient("stp-remittance-token");
		this.objectMapper = objectMapper;
		this.stpPaymentProcessingRepository = stpPaymentProcessingRepository;
		this.stpPaymentProcessingHistoryRepository = stpPaymentProcessingHistoryRepository;
		this.paymentRequestRepository = paymentRequestRepository;
		this.secretService = secretService;
		this.environment = environment;
	}

	@Override
	public void postRemittance(String providerKey, ProviderConfig providerConfig, StpVirtualCardRequestDto payload,
			String networkPaymentId, Long paymentRequestId) {
		String resolvedEndpoint = providerConfig.endpoint();
		log.info("Sending STP remittance providerKey={} endpoint={} networkPaymentId={} paymentRequestId={}",
				providerKey, resolvedEndpoint, networkPaymentId, paymentRequestId);

		if (!StringUtils.hasText(resolvedEndpoint)) {
			log.warn("STP remittance skipped: endpoint not configured for providerKey={} networkPaymentId={}",
					providerKey, networkPaymentId);
			persistOutcome(providerKey, networkPaymentId, paymentRequestId,
					StpPaymentProcessingStatus.STP_PROCESSING_FAILED, "Endpoint missing");
			return;
		}

		HttpHeaders headers = new HttpHeaders();
		headers.setContentType(MediaType.APPLICATION_JSON);
		applyOAuthIfNeeded(providerKey, providerConfig, headers);
		try {
			postOnce(providerKey, resolvedEndpoint, payload, headers, networkPaymentId);
			log.info("STP remittance succeeded: providerKey={} networkPaymentId={}", providerKey, networkPaymentId);
			persistOutcome(providerKey, networkPaymentId, paymentRequestId, StpPaymentProcessingStatus.REMITTANCE_SENT,
					"Remittance sent");
		}
		catch (STPProviderException ex) {
			log.error("STP remittance business failure: providerKey={} networkPaymentId={} status={} error={}",
					providerKey, networkPaymentId, ex.getStatusCode(), ex.getMessage());
			persistOutcome(providerKey, networkPaymentId, paymentRequestId,
					StpPaymentProcessingStatus.STP_PROCESSING_FAILED, "business failure - " + ex.getMessage());
			throw ex;
		}
		catch (STPProviderTechnicalException ex) {
			log.error("STP remittance technical failure: providerKey={} networkPaymentId={} error={}", providerKey,
					networkPaymentId, ex.getMessage());
			persistOutcome(providerKey, networkPaymentId, paymentRequestId,
					StpPaymentProcessingStatus.STP_PROCESSING_FAILED, "technical failure - " + ex.getMessage());
			throw ex;
		}
	}

	private void persistOutcome(String providerKey, String networkPaymentId, Long paymentRequestId,
			StpPaymentProcessingStatus status, String historyRemark) {
		Long resolvedPaymentRequestId = resolvePaymentRequestId(paymentRequestId, networkPaymentId);
		if (resolvedPaymentRequestId == null) {
			log.error(
					"Unable to persist STP payment processing for networkPaymentId={} due to missing paymentRequestId",
					networkPaymentId);
			return;
		}

		StpPaymentProcessing processing = this.stpPaymentProcessingRepository.findByNetworkPaymentId(networkPaymentId)
			.orElseGet(StpPaymentProcessing::new);
		processing.setPaymentRequestId(resolvedPaymentRequestId);
		processing.setNetworkPartner(providerKey);
		processing.setNetworkPaymentId(networkPaymentId);
		processing.setStatus(status);
		if (processing.getRetryCount() == null) {
			processing.setRetryCount(0);
		}
		this.stpPaymentProcessingRepository.save(processing);

		StpPaymentProcessingHistory history = new StpPaymentProcessingHistory();
		history.setNetworkPaymentId(networkPaymentId);
		history.setStatus(status);
		history.setRemarks(historyRemark);
		this.stpPaymentProcessingHistoryRepository.save(history);
	}

	private Long resolvePaymentRequestId(Long paymentRequestId, String networkPaymentId) {
		if (paymentRequestId != null) {
			return paymentRequestId;
		}
		Optional<Long> resolved = this.paymentRequestRepository.findByPaymentDetailsNetworkPaymentId(networkPaymentId)
			.map((paymentRequest) -> paymentRequest.getId());
		return resolved.orElse(null);
	}

	private void postOnce(String providerKey, String endpoint, Object payload, HttpHeaders headers,
			String networkPaymentId) {
		try {
			ResponseEntity<Object> response = this.remittanceClient.execute((client) -> client.post()
				.uri(endpoint)
				.headers((requestHeaders) -> requestHeaders.addAll(headers))
				.body(payload)
				.retrieve()
				.toEntity(Object.class));
			if (response.getStatusCode().is2xxSuccessful()) {
				log.info("STP remittance posted successfully for networkPaymentId={} providerKey={} status={}",
						networkPaymentId, providerKey, response.getStatusCode());
				return;
			}
			if (response.getStatusCode().is5xxServerError() || response.getStatusCode().is4xxClientError()) {
				throw new STPProviderTechnicalException(
						"STP remittance response status: " + response.getStatusCode().value());
			}
			throw new STPProviderException("STP remittance response status: " + response.getStatusCode().value(),
					response.getStatusCode().value());
		}
		catch (HttpStatusCodeException ex) {
			if (ex.getStatusCode().is5xxServerError() || ex.getStatusCode().is4xxClientError()) {
				throw new STPProviderTechnicalException("STP remittance exception: " + ex.getMessage(), ex);
			}
			throw new STPProviderException("STP remittance business exception: " + ex.getMessage(),
					ex.getStatusCode().value());
		}
		catch (ResourceAccessException ex) {
			throw new STPProviderTechnicalException("STP remittance transport exception: " + ex.getMessage(), ex);
		}
		catch (Exception ex) {
			throw new STPProviderTechnicalException("Unknown Exception during STP provider API: " + ex.getMessage(),
					ex);
		}
	}

	private void applyOAuthIfNeeded(String providerKey, ProviderConfig providerConfig, HttpHeaders headers) {
		if (!StpOAuthHelper.isOAuthType(providerConfig.authType())) {
			return;
		}
		String normalizedProviderKey = providerKey.toLowerCase();
		String remittancePrefix = "stp.remittance.providers." + normalizedProviderKey + ".";
		String tokenUrl = StpOAuthHelper.resolveTokenUrl(normalizedProviderKey, this.environment);
		String scope = this.environment.getProperty(remittancePrefix + "oauth.scope");
		String[] credentials = StpOAuthHelper.resolveOAuthCredentials(normalizedProviderKey, this.secretService);
		String token = StpOAuthHelper.fetchAccessToken(normalizedProviderKey, this.tokenClient, this.objectMapper,
				tokenUrl, credentials[0], credentials[1], scope);
		if (StringUtils.hasText(token)) {
			headers.setBearerAuth(token);
		}
	}

	record CachedOAuthToken(String token, long expiresAtEpochMs) {
	}

	public static class STPProviderTechnicalException extends RuntimeException {

		STPProviderTechnicalException(String message) {
			super(message);
		}

		STPProviderTechnicalException(String message, Throwable cause) {
			super(message, cause);
		}

	}

	private static class STPProviderException extends RuntimeException {

		private final int statusCode;

		STPProviderException(String message, int statusCode) {
			super(message);
			this.statusCode = statusCode;
		}

		int getStatusCode() {
			return this.statusCode;
		}

	}

}
