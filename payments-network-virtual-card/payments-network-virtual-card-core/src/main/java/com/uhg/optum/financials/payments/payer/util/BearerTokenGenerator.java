package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.config.ZetaApiProperties;
import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.constant.CommonConstants;
import com.uhg.optum.financials.payments.payer.exception.ZetaNonRetryableException;
import com.uhg.optum.financials.payments.payer.exception.ZetaRetryableException;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.lang3.StringUtils;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.core.ParameterizedTypeReference;
import org.springframework.stereotype.Component;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.web.client.HttpClientErrorException;
import org.springframework.web.client.RestClientException;

import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.Base64;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

@Component
@RequiredArgsConstructor
@Slf4j
public class BearerTokenGenerator {

	private final ZetaApiProperties zetaApiProperties;

	private final SecretService secretService;

	private final ResilientRestClientFactory resilientRestClientFactory;

	// Constants - HTTP Client
	private static final String ZETA_TOKEN_CLIENT = "zeta-token";

	// Constants - OAuth grant and scope
	private static final String GRANT_TYPE = "client_credentials";

	private static final String ADMIN_SCOPE = "admin";

	// Constants - Token response fields
	private static final String RESPONSE_FIELD_ACCESS_TOKEN = "access_token";

	private static final String RESPONSE_FIELD_VALID_TILL = "valid_till";

	private static final String RESPONSE_FIELD_EXPIRES_IN = "expires_in";

	// Constants - Token expiration
	private static final long DEFAULT_EXPIRES_IN_SECONDS = 3599L;

	private static final long TOKEN_EXPIRATION_GRACE_PERIOD_SECONDS = 10L;

	// Constants - Cache
	private static final String CACHE_KEY = AppConstants.TOKEN_CACHE_KEY.replace("'", "");

	/**
	 * Thread-safe cache for storing Zeta tokens with expiration information. This
	 * replaces the Spring @Cacheable approach to allow proactive expiration checking.
	 */
	private static final Map<String, CachedZetaToken> tokenCache = new ConcurrentHashMap<>();

	/**
	 * Generates a Zeta token with proactive expiration checking.
	 *
	 * This implementation is similar to STP's token handling: - Checks cached token
	 * before fetching a new one - Refreshes token proactively if it's expiring within 10
	 * seconds - Tracks token expiration time from the API response
	 * @return valid access token
	 * @throws ZetaRetryableException if token fetch fails with retryable error
	 * @throws ZetaNonRetryableException if token fetch fails with non-retryable error
	 */
	public String generateToken() {
		// Check if cached token is still valid (not expiring within grace period)
		CachedZetaToken cachedToken = getCachedTokenIfValid();
		if (cachedToken != null) {
			return cachedToken.token();
		}

		log.info("Fetching new token from Zeta API...");
		return fetchAndCacheNewToken();
	}

	/**
	 * Retrieves cached token if it's valid and not expiring soon.
	 * @return cached token if valid, null otherwise
	 */
	private CachedZetaToken getCachedTokenIfValid() {
		CachedZetaToken cached = tokenCache.get(CACHE_KEY);
		if (cached == null) {
			return null;
		}

		long now = System.currentTimeMillis();
		long gracePeriodMs = Duration.ofSeconds(TOKEN_EXPIRATION_GRACE_PERIOD_SECONDS).toMillis();
		long expirationThreshold = now + gracePeriodMs;

		if (cached.expiresAtEpochMs() > expirationThreshold) {
			long remainingTimeMs = cached.expiresAtEpochMs() - now;
			log.debug("Using cached Zeta token (valid for {}ms)", remainingTimeMs);
			return cached;
		}

		long expiresInMs = cached.expiresAtEpochMs() - now;
		log.debug("Cached token expired or expiring soon (expires in {}ms), fetching new token", expiresInMs);
		return null;
	}

	/**
	 * Fetches a new token from Zeta API and caches it with expiration information.
	 * @return access token
	 * @throws ZetaRetryableException if token fetch fails with retryable error
	 * @throws ZetaNonRetryableException if token fetch fails with non-retryable error
	 */
	private String fetchAndCacheNewToken() {
		try {
			HttpHeaders headers = buildAuthHeaders();
			MultiValueMap<String, Object> formData = buildTokenRequestBody();

			ResponseEntity<Map<String, Object>> response = callTokenEndpoint(headers, formData);
			validateTokenResponse(response);

			Map<String, Object> responseBody = response.getBody();
			String accessToken = extractAccessToken(responseBody);
			long expiresAt = extractTokenExpiration(responseBody);

			cacheToken(accessToken, expiresAt);
			return accessToken;
		}
		catch (HttpClientErrorException ex) {
			log.error("Client error while fetching token: {}", ex.getMessage());
			int statusCode = ex.getStatusCode().value();
			throw new ZetaNonRetryableException("Client error while calling token API: " + ex.getMessage(), statusCode);
		}
		catch (RestClientException ex) {
			log.error("Network error while fetching token: {}", ex.getMessage());
			throw new ZetaRetryableException("Error while calling token API: " + ex.getMessage(),
					CommonConstants.HTTP_503);
		}
		catch (Exception ex) {
			log.error("Unexpected error while fetching token: {}", ex.getMessage(), ex);
			throw new ZetaRetryableException("Unexpected error while fetching token: " + ex.getMessage(),
					CommonConstants.HTTP_500);
		}
	}

	/**
	 * Builds HTTP headers for OAuth token request with Basic authentication.
	 * @return configured HTTP headers
	 */
	private HttpHeaders buildAuthHeaders() {
		HttpHeaders headers = new HttpHeaders();
		headers.setContentType(MediaType.MULTIPART_FORM_DATA);

		String clientId = resolveClientId();
		String clientSecret = resolveClientSecret();

		if (StringUtils.isNotBlank(clientId) && StringUtils.isNotBlank(clientSecret)) {
			String credentials = clientId + ":" + clientSecret;
			String base64Creds = Base64.getEncoder().encodeToString(credentials.getBytes(StandardCharsets.UTF_8));
			headers.set("Authorization", "Basic " + base64Creds);
		}

		return headers;
	}

	/**
	 * Resolves client ID from properties or vault.
	 * @return client ID value
	 */
	private String resolveClientId() {
		return StringUtils.defaultIfEmpty(this.zetaApiProperties.getClientId(),
				this.secretService.getSecretUsingKey(AppConstants.ZETA_CLIENT_ID));
	}

	/**
	 * Resolves client secret from properties or vault.
	 * @return client secret value
	 */
	private String resolveClientSecret() {
		return StringUtils.defaultIfEmpty(this.zetaApiProperties.getClientSecret(),
				this.secretService.getSecretUsingKey(AppConstants.ZETA_CLIENT_SECRET));
	}

	/**
	 * Builds OAuth 2.0 client credentials token request body.
	 * @return form data for token request
	 */
	private MultiValueMap<String, Object> buildTokenRequestBody() {
		MultiValueMap<String, Object> formData = new LinkedMultiValueMap<>();
		formData.add("grant_type", GRANT_TYPE);
		formData.add("scope", ADMIN_SCOPE);
		return formData;
	}

	/**
	 * Calls Zeta token endpoint.
	 * @param headers http headers for the request
	 * @param formData form data for the request body
	 * @return response entity containing token data
	 */
	private ResponseEntity<Map<String, Object>> callTokenEndpoint(HttpHeaders headers,
			MultiValueMap<String, Object> formData) {
		ResilientRestClient tokenClient = this.resilientRestClientFactory.getClient(ZETA_TOKEN_CLIENT);
		return tokenClient.execute((restClient) -> restClient.post()
			.uri(this.zetaApiProperties.getTokenUrl())
			.headers((requestHeaders) -> requestHeaders.addAll(headers))
			.body(formData)
			.retrieve()
			.toEntity(new ParameterizedTypeReference<>() {
			}));
	}

	/**
	 * Validates token response from Zeta API.
	 * @param response the response entity to validate
	 * @throws ZetaRetryableException if response is not successful or body is null
	 */
	private void validateTokenResponse(ResponseEntity<Map<String, Object>> response) {
		if (!response.getStatusCode().is2xxSuccessful()) {
			throw new ZetaRetryableException(
					"Unexpected response while fetching token. Status: " + response.getStatusCode(),
					response.getStatusCode().value());
		}
		if (response.getBody() == null || response.getBody().isEmpty()) {
			throw new ZetaRetryableException("Token response body is null or empty", CommonConstants.HTTP_500);
		}
	}

	/**
	 * Extracts access token from response body.
	 * @param responseBody the response body map
	 * @return the access token string
	 * @throws ZetaRetryableException if access token is missing or invalid
	 */
	private String extractAccessToken(Map<String, Object> responseBody) {
		if (responseBody == null) {
			throw new ZetaRetryableException("Token response body is null", CommonConstants.HTTP_500);
		}
		Object tokenObj = responseBody.get(RESPONSE_FIELD_ACCESS_TOKEN);
		if (tokenObj instanceof String token && StringUtils.isNotBlank(token)) {
			return token;
		}
		throw new ZetaRetryableException("Token response missing or invalid access_token field",
				CommonConstants.HTTP_500);
	}

	/**
	 * Extracts token expiration time from response. Prefers server-provided valid_till if
	 * available, otherwise calculates from expires_in.
	 * @param responseBody the response body map
	 * @return expiration time in epoch milliseconds
	 */
	private long extractTokenExpiration(Map<String, Object> responseBody) {
		// Prefer server-provided valid_till (epoch ms) if available
		Object validTill = responseBody.get(RESPONSE_FIELD_VALID_TILL);
		if (validTill instanceof Number) {
			long expiresAt = ((Number) validTill).longValue();
			log.debug("Using server-provided token expiration: valid_till={}", expiresAt);
			return expiresAt;
		}

		// Fallback: calculate expiration from expires_in (in seconds)
		long now = System.currentTimeMillis();
		Number expiresIn = (Number) responseBody.getOrDefault(RESPONSE_FIELD_EXPIRES_IN, DEFAULT_EXPIRES_IN_SECONDS);
		long expiresAt = now + (expiresIn.longValue() * 1000);
		log.debug("Calculated token expiration from expires_in: {}s", expiresIn.longValue());
		return expiresAt;
	}

	/**
	 * Caches the token with its expiration time.
	 * @param token the access token to cache
	 * @param expiresAt the expiration time in epoch milliseconds
	 */
	private void cacheToken(String token, long expiresAt) {
		tokenCache.put(CACHE_KEY, new CachedZetaToken(token, expiresAt));
		long expiresInSeconds = (expiresAt - System.currentTimeMillis()) / 1000;
		log.info("Zeta token successfully fetched and cached, expires in ~{}s", expiresInSeconds);
	}

	/**
	 * Evicts the cached Zeta token, forcing a fresh token fetch on next request. Called
	 * when receiving 401/403 responses from Zeta API.
	 */
	public void evictToken() {
		tokenCache.remove(CACHE_KEY);
		log.info("Evicting cached Zeta token manually.");
	}

	/**
	 * Cached token record similar to STP implementation, storing both the token and its
	 * expiration time in epoch milliseconds.
	 *
	 * @param token the access token string
	 * @param expiresAtEpochMs the expiration time in epoch milliseconds
	 */
	record CachedZetaToken(String token, long expiresAtEpochMs) {
	}

}
