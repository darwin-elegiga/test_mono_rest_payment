package com.uhg.optum.financials.payments.payer.service.impl;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.core.env.Environment;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.util.StringUtils;

import java.time.Duration;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Package-private helper for fetching OAuth client_credentials access tokens. Used by
 * both remittance and enrollment callback flows.
 *
 * @author md shai234
 */
@Slf4j
final class StpOAuthHelper {

	static final long DEFAULT_EXPIRES_IN_SECONDS = 300L;

	/**
	 * Common credential prefix: client-id and client-secret are resolved from here for
	 * both enrollment and remittance. e.g.
	 * {@code stp.enrollment.providers.stp-partner.oauth.client-id}
	 */
	static final String CREDENTIAL_PREFIX = "stp.enrollment.providers.";

	/**
	 * Prefix used when resolving OAuth client-id/secret from {@link SecretService} (Azure
	 * Key Vault). Key Vault secret names only allow alphanumeric characters and dashes
	 * (no dots) - same convention as other vault secrets in this app (e.g.
	 * {@code zeta-client-id}). e.g.
	 * {@code stp-enrollment-providers-stp-partner-oauth-client-id}
	 */
	static final String VAULT_CREDENTIAL_PREFIX = "stp-enrollment-providers-";

	/**
	 * Fallback client-id used when the OAuth client-id is not configured/found in
	 * {@link SecretService} (e.g. local/dev environments without Key Vault access), so
	 * that OAuth token requests are never sent with a blank/missing client-id.
	 */
	static final String DEFAULT_CLIENT_ID = "default-client-id";

	/**
	 * Fallback client-secret used when the OAuth client-secret is not configured/found in
	 * {@link SecretService} (e.g. local/dev environments without Key Vault access), so
	 * that OAuth token requests are never sent with a blank/missing client-secret.
	 */
	static final String DEFAULT_CLIENT_SECRET = "default-client-secret";

	private static final TypeReference<Map<String, Object>> MAP_TYPE = new TypeReference<>() {
	};

	private static final Map<String, StpProviderClientImpl.CachedOAuthToken> tokenCache = new ConcurrentHashMap<>();

	private StpOAuthHelper() {
	}

	private static String normalize(String value) {
		return (value != null) ? value.trim() : null;
	}

	/**
	 * Common entry point used by <strong>both</strong> enrollment and remittance flows to
	 * resolve the OAuth token URL from the {@link Environment}.
	 * <p>
	 * Always uses the credential prefix:
	 * {@code stp.enrollment.providers.{providerKey}.oauth.token-url} regardless of which
	 * flow (enrollment vs remittance) is calling.
	 */
	static String resolveTokenUrl(String providerKey, Environment environment) {
		String prefix = CREDENTIAL_PREFIX + providerKey + ".";
		return environment.getProperty(prefix + "oauth.token-url");
	}

	/**
	 * Common entry point used by <strong>both</strong> enrollment and remittance flows to
	 * resolve OAuth client credentials from {@link SecretService}.
	 * <p>
	 * Always uses the credential prefix:
	 * {@code stp.enrollment.providers.{providerKey}.oauth.client-id/secret} regardless of
	 * which flow (enrollment vs remittance) is calling.
	 * <p>
	 * If the credential is not configured/found in the vault, falls back to
	 * {@link #DEFAULT_CLIENT_ID}/{@link #DEFAULT_CLIENT_SECRET} instead of returning
	 * {@code null}, so OAuth token requests always include a client-id/client-secret.
	 * @return array of [clientId, clientSecret]; never {@code null} or blank
	 */
	static String[] resolveOAuthCredentials(String providerKey, SecretService secretService) {
		String vaultPrefix = VAULT_CREDENTIAL_PREFIX + providerKey + "-";
		String clientIdSecretPath = vaultPrefix + "oauth-client-id";
		String clientSecretSecretPath = vaultPrefix + "oauth-client-secret";

		String clientId = null;
		String clientSecret = null;

		if (secretService != null) {
			clientId = fetchSecretSafely(secretService, clientIdSecretPath, providerKey, "client-id");
			clientSecret = fetchSecretSafely(secretService, clientSecretSecretPath, providerKey, "client-secret");
		}

		if (!StringUtils.hasText(clientId)) {
			log.warn("OAuth client-id not available from SecretService for provider key={}; using default value",
					providerKey);
			clientId = DEFAULT_CLIENT_ID;
		}
		if (!StringUtils.hasText(clientSecret)) {
			log.warn("OAuth client-secret not available from SecretService for provider key={}; using default value",
					providerKey);
			clientSecret = DEFAULT_CLIENT_SECRET;
		}

		return new String[] { clientId, clientSecret };
	}

	/**
	 * Fetches a single secret from {@link SecretService}, tolerating both a
	 * blank/{@code null} return value and a thrown exception (e.g. Azure Key Vault's
	 * {@code ResourceNotFoundException} when the secret does not exist) - either case is
	 * treated as "not configured" so a missing OAuth secret never crashes message
	 * processing.
	 */
	private static String fetchSecretSafely(SecretService secretService, String secretKey, String providerKey,
			String credentialLabel) {
		String normalizedKey = normalize(secretKey);
		if (!StringUtils.hasText(normalizedKey)) {
			return null;
		}
		try {
			String value = normalize(secretService.getSecretUsingKey(normalizedKey));
			if (!StringUtils.hasText(value)) {
				log.warn("OAuth {} not found in vault for provider key={}", credentialLabel, providerKey);
				return null;
			}
			return value;
		}
		catch (Exception ex) {
			log.warn("OAuth {} could not be retrieved from vault for provider key={} error={}", credentialLabel,
					providerKey, ex.getMessage());
			return null;
		}
	}

	/**
	 * Posts to the OAuth token endpoint using client_credentials grant type.
	 * <p>
	 * Note: clientId and clientSecret should already be resolved (not paths). Use
	 * {@link #resolveOAuthCredentials} to resolve credentials beforehand.
	 * @param clientId the already-resolved client ID
	 * @param clientSecret the already-resolved client secret
	 * @return the access_token string, or {@code null} if the call fails
	 */
	static String fetchAccessToken(String providerKey, ResilientRestClient resilientRestClient,
			ObjectMapper objectMapper, String tokenUrl, String clientId, String clientSecret, String scope) {
		if (!StringUtils.hasText(tokenUrl)) {
			log.warn("Skipping OAuth token fetch due to missing tokenUrl for provider key={}", providerKey);
			return null;
		}

		StpProviderClientImpl.CachedOAuthToken cached = tokenCache.get(providerKey);
		long now = System.currentTimeMillis();
		if (cached != null && cached.expiresAtEpochMs() > now + Duration.ofSeconds(10).toMillis()) {
			log.debug("OAuth token served from cache for provider key={}", providerKey);
			return cached.token();
		}
		HttpHeaders headers = new HttpHeaders();
		headers.setContentType(MediaType.APPLICATION_FORM_URLENCODED);
		if (StringUtils.hasText(clientId) && StringUtils.hasText(clientSecret)) {
			headers.setBasicAuth(clientId, clientSecret);
		}
		MultiValueMap<String, String> form = new LinkedMultiValueMap<>();
		form.add("grant_type", "client_credentials");
		if (StringUtils.hasText(clientId)) {
			form.add("client_id", clientId);
		}
		if (StringUtils.hasText(clientSecret)) {
			form.add("client_secret", clientSecret);
		}
		if (StringUtils.hasText(scope)) {
			form.add("scope", scope);
		}
		try {
			ResponseEntity<String> response = resilientRestClient.execute((restClient) -> restClient.post()
				.uri(tokenUrl)
				.headers((requestHeaders) -> requestHeaders.addAll(headers))
				.body(form)
				.retrieve()
				.toEntity(String.class));
			if (!response.getStatusCode().is2xxSuccessful() || !StringUtils.hasText(response.getBody())) {
				log.error("Failed to fetch OAuth token for provider key={} status={}", providerKey,
						response.getStatusCode());
				return null;
			}
			Map<String, Object> tokenResponse = objectMapper.readValue(response.getBody(), MAP_TYPE);
			String accessToken = (tokenResponse.get("access_token") instanceof String s) ? s.trim() : null;
			Number expiresIn = (Number) tokenResponse.getOrDefault("expires_in", 300);
			if (!StringUtils.hasText(accessToken)) {
				log.error("OAuth token response missing access_token for provider key={}", providerKey);
				return null;
			}
			long expiresAt = now + (expiresIn.longValue() * 1000);
			tokenCache.put(providerKey, new StpProviderClientImpl.CachedOAuthToken(accessToken, expiresAt));
			log.info("OAuth token successfully fetched and cached for provider key={} expiresIn={}s", providerKey,
					expiresIn.longValue());
			return accessToken;
		}
		catch (Exception ex) {
			log.error("OAuth token fetch exception: tokenUrl={} error={}", tokenUrl, ex.getMessage());
			return null;
		}
	}

	/**
	 * Returns {@code true} if the given auth-type string indicates OAuth
	 * client_credentials.
	 */
	static boolean isOAuthType(String authType) {
		return "oauth".equalsIgnoreCase(authType) || "client_credentials".equalsIgnoreCase(authType);
	}

}
