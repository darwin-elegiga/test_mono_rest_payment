package com.uhg.optum.financials.payments.payer.service.impl;

import com.google.gson.Gson;
import com.uhg.optum.financials.payments.payer.config.ZetaApiProperties;
import com.uhg.optum.financials.payments.payer.dto.CancelCardRequest;
import com.uhg.optum.financials.payments.payer.dto.CardDetailsDTO;
import com.uhg.optum.financials.payments.payer.dto.CreateCardRequest;
import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import com.uhg.optum.financials.payments.payer.dto.ResponseObject;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.BearerTokenGenerator;
import com.uhg.optum.financials.shared.core.http.resilience.DownstreamUnavailableException;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.lang3.StringUtils;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpMethod;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import tools.jackson.databind.ObjectMapper;
import tools.jackson.databind.node.ObjectNode;

import java.util.Map;
import java.util.Objects;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD_GUID;

@Slf4j
@Service
@RequiredArgsConstructor
public class ZetaApiServiceImpl implements ZetaApiService {

	private final BearerTokenGenerator bearerTokenGenerator;

	private final ZetaApiProperties zetaApiProperties;

	private final RetryableApiService<Object> apiService;

	private final ObjectMapper objectMapper;

	private final RetryableApiService<Object> retryableApiService;

	private final ResilientRestClientFactory resilientRestClientFactory;

	private static final String CARD_LOOKUP_CLIENT = "zeta-card-lookup";

	private static final String CARD_BALANCE_CLIENT = "zeta-card-balance";

	private static final Gson JSON = new Gson();

	@Override
	public ResponseEntity<String> getVirtualCard(UUID cardId) throws Exception {
		Map<String, String> queryParams = Map.of("view", "EXPANDED");

		String url = Objects.requireNonNull(this.zetaApiProperties.getGetCardUrl(), "Invalid URL")
			.replace("{cardGuid}", cardId.toString());
		StringBuilder urlBuilder = new StringBuilder(url);

		queryParams.forEach((key, value) -> urlBuilder.append("?").append(key).append("=").append(value));
		HttpHeaders httpHeaders = new HttpHeaders();
		httpHeaders.add("X-Zeta-AuthToken", this.bearerTokenGenerator.generateToken());
		ResponseEntity<String> response;
		try {
			ResilientRestClient client = this.resilientRestClientFactory.getClient(CARD_LOOKUP_CLIENT);
			response = client.execute((restClient) -> restClient.get()
				.uri(urlBuilder.toString())
				.headers((headers) -> headers.addAll(httpHeaders))
				.retrieve()
				.toEntity(String.class));
		}
		catch (DownstreamUnavailableException ex) {
			response = ResponseEntity.status(503).body(ex.getMessage());
		}
		catch (org.springframework.web.client.RestClientResponseException ex) {
			response = (ex.getResponseBodyAsString() != null)
					? ResponseEntity.status(ex.getStatusCode()).body(ex.getResponseBodyAsString())
					: ResponseEntity.status(ex.getStatusCode()).build();
		}
		return response;
	}

	@Override
	public ResponseEntity<?> postVirtualCard(CreateCardRequest createCardRequest) throws Exception {
		String bearerToken = this.bearerTokenGenerator.generateToken();
		String url = this.zetaApiProperties.getCreateCardUrl();

		ResponseObject responseObject = (ResponseObject) this.apiService.callZetaApiWithRetry(createCardRequest,
				bearerToken, url);

		return ResponseEntity.status(responseObject.getHttpStatusCode()).body(responseObject);
	}

	@Override
	public ResponseEntity<?> cancelVirtualCard(CancelCardRequest cancelCardRequest) throws Exception {

		String bearerToken = this.bearerTokenGenerator.generateToken();
		String url = this.zetaApiProperties.getCancelCardUrl()
			.replace(CARD_GUID, String.valueOf(cancelCardRequest.getCardGuid()));

		ObjectNode filteredCancelCardRequest = this.objectMapper.valueToTree(cancelCardRequest);
		filteredCancelCardRequest.remove("cardGuid");

		ResponseObject responseObject = (ResponseObject) this.apiService.callZetaApiWithRetry(filteredCancelCardRequest,
				bearerToken, url);

		return ResponseEntity.status(responseObject.getHttpStatusCode()).body(responseObject);
	}

	@Override
	public CardDetailsDTO getCardDetails(UUID cardId) throws Exception {
		ResponseEntity<String> response = getVirtualCard(cardId);
		String body = response.getBody();
		if (body == null || body.isEmpty()) {
			throw new RuntimeException("Response body is null or empty");
		}

		CardDetailsDTO cardDetails = JSON.fromJson(response.getBody(), CardDetailsDTO.class);

		return cardDetails;
	}

	@Override
	public ResponseEntity<?> resetVirtualCard(ResetCardRequest resetCardRequest) throws Exception {

		String bearerToken = this.bearerTokenGenerator.generateToken();
		String url = this.zetaApiProperties.getResetCardUrl()
			.replace(CARD_GUID, String.valueOf(resetCardRequest.getCardGuid()));

		ObjectNode filteredResetCardRequest = this.objectMapper.valueToTree(resetCardRequest);
		filteredResetCardRequest.remove("cardGuid");
		filteredResetCardRequest.remove("paymentId");

		ResponseObject responseObject = (ResponseObject) this.apiService.callZetaApiWithRetry(filteredResetCardRequest,
				bearerToken, url);

		return ResponseEntity.status(responseObject.getHttpStatusCode()).body(responseObject);
	}

	@Override
	public ResponseEntity<String> getCardCreationStatus(UUID requestId) throws Exception {
		String url = this.zetaApiProperties.getCreateCardUrl();
		String bearerToken = this.bearerTokenGenerator.generateToken();

		String urlBuilder = url + "?requestId=" + requestId.toString();
		return (ResponseEntity<String>) this.retryableApiService.callZetaApiWithRetry(CARD_BALANCE_CLIENT, null,
				bearerToken, urlBuilder, HttpMethod.GET.name(), String.class);

	}

	@Override
	public ResponseEntity<String> getCardBalance(UUID cguid) throws Exception {
		String url = this.zetaApiProperties.getGetCardBalanceUrl();
		String bearerToken = this.bearerTokenGenerator.generateToken();

		String urlBuilder = url.replace("{cguid}", cguid.toString());
		return (ResponseEntity<String>) this.retryableApiService.callZetaApiWithRetry(null, bearerToken, urlBuilder,
				HttpMethod.GET.name(), String.class);
	}

	@Override
	public ResponseEntity<String> getAllRules(Integer page, Integer size, String ruleTpe) throws Exception {
		String bearerToken = this.bearerTokenGenerator.generateToken();
		String url = buildUrlToGetAllRules(ruleTpe, page, size);
		return (ResponseEntity<String>) this.retryableApiService.callZetaApiWithRetry(null, bearerToken, url,
				HttpMethod.GET.name(), String.class);
	}

	@Override
	public ResponseEntity<String> getRuleById(String ruleId) throws Exception {
		String url = this.zetaApiProperties.getGetRuleByRuleIdUrl();
		String bearerToken = this.bearerTokenGenerator.generateToken();

		String urlBuilder = url.replace("{ruleId}", ruleId);
		return (ResponseEntity<String>) this.retryableApiService.callZetaApiWithRetry(null, bearerToken,
				urlBuilder.toString(), HttpMethod.GET.name(), String.class);
	}

	private String buildUrlToGetAllRules(String ruleType, Integer page, Integer size) throws Exception {
		String url = this.zetaApiProperties.getGetAllRulesUrl();
		StringBuilder urlBuilder = new StringBuilder(url);

		if (StringUtils.isEmpty(ruleType)) {
			urlBuilder.append("?page=").append(page).append("&size=").append(size);
		}
		else {
			urlBuilder.append("?ruleType=")
				.append(ruleType)
				.append("&page=")
				.append(page)
				.append("&size=")
				.append(size);
		}
		return urlBuilder.toString();
	}

}
