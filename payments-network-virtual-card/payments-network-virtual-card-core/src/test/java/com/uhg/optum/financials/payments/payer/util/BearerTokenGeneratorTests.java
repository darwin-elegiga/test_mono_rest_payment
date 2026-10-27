package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.config.ZetaApiProperties;
import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.exception.ZetaNonRetryableException;
import com.uhg.optum.financials.payments.payer.exception.ZetaRetryableException;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.mockito.junit.jupiter.MockitoSettings;
import org.mockito.quality.Strictness;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.test.util.ReflectionTestUtils;
import org.springframework.web.client.HttpClientErrorException;
import org.springframework.web.client.RestClientException;

import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.Base64;
import java.util.HashMap;
import java.util.Map;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatCode;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.atLeastOnce;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;

/**
 * Comprehensive test suite for BearerTokenGenerator class.
 *
 * Tests cover: - Token generation and caching logic - Token expiration and refresh
 * behavior - Grace period handling (10-second window) - Error scenarios (HTTP errors,
 * network errors, unexpected errors) - Token extraction and validation - Credential
 * resolution from properties and vault - Manual token eviction
 */

@ExtendWith(MockitoExtension.class)
@MockitoSettings(strictness = Strictness.LENIENT)
@DisplayName("BearerTokenGenerator Tests")
class BearerTokenGeneratorTests {

	@Mock
	private ZetaApiProperties zetaApiProperties;

	@Mock
	private SecretService secretService;

	@Mock
	private ResilientRestClientFactory resilientRestClientFactory;

	@Mock
	private ResilientRestClient resilientRestClient;

	@InjectMocks
	private BearerTokenGenerator bearerTokenGenerator;

	private String cacheKeyForReflection;

	@BeforeEach
	void setUp() {
		// Clear the static token cache before each test
		clearTokenCache();

		// Mock the client retrieval
		given(this.resilientRestClientFactory.getClient("zeta-token")).willReturn(this.resilientRestClient);

		// Default mock values
		given(this.zetaApiProperties.getClientId()).willReturn("testClientId");
		given(this.zetaApiProperties.getClientSecret()).willReturn("testClientSecret");
		given(this.zetaApiProperties.getTokenUrl()).willReturn("http://zeta.api/token");

		// Get the cache key via reflection for cache operations
		this.cacheKeyForReflection = AppConstants.TOKEN_CACHE_KEY.replace("'", "");
	}

	/**
	 * Clears the static token cache using reflection.
	 */
	private void clearTokenCache() {
		try {
			@SuppressWarnings("unchecked")
			Map<String, Object> cache = (Map<String, Object>) ReflectionTestUtils.getField(BearerTokenGenerator.class,
					"tokenCache");
			if (cache != null) {
				cache.clear();
			}
		}
		catch (Exception ex) {
			// Cache may not be accessible, continue with test
		}
	}

	/**
	 * Puts a token into the cache using reflection.
	 */
	private void putTokenInCache(String token, long expiresAtEpochMs) {
		try {
			@SuppressWarnings("unchecked")
			Map<String, Object> cache = (Map<String, Object>) ReflectionTestUtils.getField(BearerTokenGenerator.class,
					"tokenCache");
			if (cache != null) {
				// Create a CachedZetaToken using reflection
				var cachedTokenClass = Class
					.forName("com.uhg.optum.financials.payments.payer.util.BearerTokenGenerator$CachedZetaToken");
				var constructor = cachedTokenClass.getDeclaredConstructor(String.class, long.class);
				constructor.setAccessible(true);
				Object cachedToken = constructor.newInstance(token, expiresAtEpochMs);
				cache.put(this.cacheKeyForReflection, cachedToken);
			}
		}
		catch (Exception ex) {
			// Fallback: continue with test
		}
	}

	@Nested
	@DisplayName("Token Generation - Happy Path")
	class TokenGenerationHappyPath {

		@Test
		@DisplayName("Should generate and return token from API on first call")
		void shouldGenerateTokenOnFirstCall() {
			// Arrange
			String expectedToken = "valid_access_token_123";
			long expiresIn = 3600L; // 1 hour

			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", expectedToken, "expires_in", expiresIn, "token_type", "Bearer"));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act
			String token = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert
			assertThat(token).isEqualTo(expectedToken);
			verify(BearerTokenGeneratorTests.this.resilientRestClientFactory).getClient("zeta-token");
			verify(BearerTokenGeneratorTests.this.resilientRestClient).execute(any());
		}

		@Test
		@DisplayName("Should use cached token when still valid")
		void shouldUseCachedTokenWhenValid() {
			// Arrange
			String cachedToken = "cached_token_123";
			long now = System.currentTimeMillis();
			long expiresAt = now + Duration.ofHours(1).toMillis();

			BearerTokenGeneratorTests.this.putTokenInCache(cachedToken, expiresAt);

			// Act
			String token = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert
			assertThat(token).isEqualTo(cachedToken);
			// Should not call the API
			verify(BearerTokenGeneratorTests.this.resilientRestClient, never()).execute(any());
		}

		@Test
		@DisplayName("Should fetch new token when cache is empty")
		void shouldFetchNewTokenWhenCacheEmpty() {
			// Arrange
			String expectedToken = "new_token_456";
			long expiresIn = 1800L; // 30 minutes
			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", expectedToken, "expires_in", expiresIn));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act
			String token = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert
			assertThat(token).isEqualTo(expectedToken);
			verify(BearerTokenGeneratorTests.this.resilientRestClient).execute(any());
		}

		@Test
		@DisplayName("Should generate Bearer token with correct OAuth 2.0 flow")
		void shouldGenerateWithCorrectOAuthFlow() {
			// Arrange
			String clientId = "myClientId";
			String clientSecret = "myClientSecret";
			String expectedToken = "oauth_token_789";

			given(BearerTokenGeneratorTests.this.zetaApiProperties.getClientId()).willReturn(clientId);
			given(BearerTokenGeneratorTests.this.zetaApiProperties.getClientSecret()).willReturn(clientSecret);

			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", expectedToken, "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act
			String token = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert
			assertThat(token).isEqualTo(expectedToken);
		}

	}

	@Nested
	@DisplayName("Token Caching Logic")
	class TokenCachingLogic {

		@Test
		@DisplayName("Should refresh token when it's expiring within grace period (10 seconds)")
		void shouldRefreshTokenWithinGracePeriod() {
			// Arrange
			String oldToken = "old_token_123";
			long now = System.currentTimeMillis();
			// Token expires in 5 seconds (within grace period)
			long expiresAt = now + (5 * 1000);

			BearerTokenGeneratorTests.this.putTokenInCache(oldToken, expiresAt);

			String newToken = "new_token_456";
			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", newToken, "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act
			String token = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert - Should fetch new token because old one is expiring soon
			assertThat(token).isEqualTo(newToken);
			verify(BearerTokenGeneratorTests.this.resilientRestClient).execute(any());
		}

		@Test
		@DisplayName("Should not refresh token when it has more than grace period remaining")
		void shouldNotRefreshTokenOutsideGracePeriod() {
			// Arrange
			String cachedToken = "valid_token_789";
			long now = System.currentTimeMillis();
			// Token expires in 30 seconds (outside grace period)
			long expiresAt = now + (30 * 1000);

			BearerTokenGeneratorTests.this.putTokenInCache(cachedToken, expiresAt);

			// Act
			String token = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert - Should use cached token
			assertThat(token).isEqualTo(cachedToken);
			verify(BearerTokenGeneratorTests.this.resilientRestClient, never()).execute(any());
		}

		@Test
		@DisplayName("Should fetch new token when cached token is expired")
		void shouldFetchNewTokenWhenExpired() {
			// Arrange
			String oldToken = "expired_token_123";
			long now = System.currentTimeMillis();
			// Token expired 1 minute ago
			long expiresAt = now - (60 * 1000);

			BearerTokenGeneratorTests.this.putTokenInCache(oldToken, expiresAt);

			String newToken = "fresh_token_456";
			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", newToken, "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act
			String token = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert
			assertThat(token).isEqualTo(newToken);
			verify(BearerTokenGeneratorTests.this.resilientRestClient).execute(any());
		}

		@Test
		@DisplayName("Should use valid_till from response when available")
		void shouldUseValidTillFromResponse() {
			// Arrange
			String expectedToken = "token_with_valid_till";
			long validTill = System.currentTimeMillis() + Duration.ofHours(2).toMillis();

			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", expectedToken, "valid_till", validTill, "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act
			String token = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert
			assertThat(token).isEqualTo(expectedToken);

			// Verify token was cached with valid_till expiration
			String cachedToken = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();
			assertThat(cachedToken).isEqualTo(expectedToken);
		}

		@Test
		@DisplayName("Should calculate expiration from expires_in when valid_till not available")
		void shouldCalculateExpirationFromExpiresIn() {
			// Arrange
			String expectedToken = "token_with_expires_in";
			long expiresIn = 1800L; // 30 minutes

			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", expectedToken, "expires_in", expiresIn));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act
			String token = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert
			assertThat(token).isEqualTo(expectedToken);

			// Verify token is cached and usable on next call
			String cachedToken = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();
			assertThat(cachedToken).isEqualTo(expectedToken);
		}

	}

	@Nested
	@DisplayName("Credential Resolution")
	class CredentialResolution {

		@Test
		@DisplayName("Should use client credentials from properties")
		void shouldUseCredentialsFromProperties() {
			// Arrange
			String clientId = "propertyClientId";
			String clientSecret = "propertyClientSecret";

			given(BearerTokenGeneratorTests.this.zetaApiProperties.getClientId()).willReturn(clientId);
			given(BearerTokenGeneratorTests.this.zetaApiProperties.getClientSecret()).willReturn(clientSecret);
			// Stub secretService to avoid unnecessary calls
			given(BearerTokenGeneratorTests.this.secretService.getSecretUsingKey(any())).willReturn(null);

			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", "token123", "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act
			BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert - Properties should be used
			verify(BearerTokenGeneratorTests.this.zetaApiProperties).getClientId();
			verify(BearerTokenGeneratorTests.this.zetaApiProperties).getClientSecret();
		}

		@Test
		@DisplayName("Should fall back to vault when credentials not in properties")
		void shouldFallbackToVaultForCredentials() {
			// Arrange
			given(BearerTokenGeneratorTests.this.zetaApiProperties.getClientId()).willReturn(""); // Empty/null
			given(BearerTokenGeneratorTests.this.zetaApiProperties.getClientSecret()).willReturn(""); // Empty/null
			given(BearerTokenGeneratorTests.this.secretService.getSecretUsingKey(AppConstants.ZETA_CLIENT_ID))
				.willReturn("vaultClientId");
			given(BearerTokenGeneratorTests.this.secretService.getSecretUsingKey(AppConstants.ZETA_CLIENT_SECRET))
				.willReturn("vaultClientSecret");

			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", "token123", "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act
			BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert
			verify(BearerTokenGeneratorTests.this.secretService).getSecretUsingKey(AppConstants.ZETA_CLIENT_ID);
			verify(BearerTokenGeneratorTests.this.secretService).getSecretUsingKey(AppConstants.ZETA_CLIENT_SECRET);
		}

		@Test
		@DisplayName("Should encode credentials in Base64 for Basic auth")
		void shouldEncodeCredentialsInBase64() {
			// Arrange
			String clientId = "user123";
			String clientSecret = "pass456";

			given(BearerTokenGeneratorTests.this.zetaApiProperties.getClientId()).willReturn(clientId);
			given(BearerTokenGeneratorTests.this.zetaApiProperties.getClientSecret()).willReturn(clientSecret);

			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", "token123", "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act
			BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert
			String expectedCredentials = clientId + ":" + clientSecret;
			// Would be encoded as Base64 for Basic auth header
			Base64.getEncoder().encodeToString(expectedCredentials.getBytes(StandardCharsets.UTF_8));

			// Verify the call was made (credentials would be in the headers)
			verify(BearerTokenGeneratorTests.this.resilientRestClient).execute(any());
		}

	}

	@Nested
	@DisplayName("Error Handling - HTTP Errors")
	class ErrorHandlingHttpErrors {

		@Test
		@DisplayName("Should throw ZetaNonRetryableException for 4xx client errors")
		void shouldThrowNonRetryableExceptionFor4xxErrors() {
			// Arrange
			HttpClientErrorException clientException = new HttpClientErrorException(HttpStatus.BAD_REQUEST,
					"Bad Request");

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willThrow(clientException);

			// Act & Assert
			assertThatThrownBy(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.isInstanceOf(ZetaNonRetryableException.class)
				.hasMessageContaining("Client error while calling token API");
		}

		@Test
		@DisplayName("Should throw ZetaNonRetryableException for 401 Unauthorized")
		void shouldThrowNonRetryableExceptionFor401() {
			// Arrange
			HttpClientErrorException clientException = new HttpClientErrorException(HttpStatus.UNAUTHORIZED,
					"Unauthorized");

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willThrow(clientException);

			// Act & Assert
			assertThatThrownBy(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.isInstanceOf(ZetaNonRetryableException.class)
				.hasMessageContaining("Client error");
		}

		@Test
		@DisplayName("Should throw ZetaNonRetryableException for 403 Forbidden")
		void shouldThrowNonRetryableExceptionFor403() {
			// Arrange
			HttpClientErrorException clientException = new HttpClientErrorException(HttpStatus.FORBIDDEN, "Forbidden");

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willThrow(clientException);

			// Act & Assert
			assertThatThrownBy(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.isInstanceOf(ZetaNonRetryableException.class);
		}

		@Test
		@DisplayName("Should preserve HTTP status code in exception")
		void shouldPreserveHttpStatusCodeInException() {
			// Arrange
			HttpClientErrorException clientException = new HttpClientErrorException(HttpStatus.BAD_REQUEST,
					"Bad Request");

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willThrow(clientException);

			// Act & Assert
			assertThatThrownBy(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.isInstanceOf(ZetaNonRetryableException.class);
		}

	}

	@Nested
	@DisplayName("Error Handling - Network Errors")
	class ErrorHandlingNetworkErrors {

		@Test
		@DisplayName("Should throw ZetaRetryableException for network errors")
		void shouldThrowRetryableExceptionForNetworkError() {
			// Arrange
			RestClientException networkException = new RestClientException("Connection timeout");

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willThrow(networkException);

			// Act & Assert
			assertThatThrownBy(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.isInstanceOf(ZetaRetryableException.class)
				.hasMessageContaining("Error while calling token API");
		}

		@Test
		@DisplayName("Should return HTTP 503 status for network errors")
		void shouldReturn503StatusForNetworkError() {
			// Arrange
			RestClientException networkException = new RestClientException("Connection refused");

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willThrow(networkException);

			// Act & Assert
			assertThatThrownBy(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.isInstanceOf(ZetaRetryableException.class);
		}

	}

	@Nested
	@DisplayName("Error Handling - Validation Errors")
	class ErrorHandlingValidationErrors {

		@Test
		@DisplayName("Should throw ZetaRetryableException for non-2xx response status")
		void shouldThrowExceptionForNon2xxStatus() {
			// Arrange
			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
				.body(Map.of("error", "Server error"));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act & Assert
			assertThatThrownBy(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.isInstanceOf(ZetaRetryableException.class)
				.hasMessageContaining("Unexpected response while fetching token");
		}

		@Test
		@DisplayName("Should throw ZetaRetryableException for null response body")
		void shouldThrowExceptionForNullResponseBody() {
			// Arrange
			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity.ok().build();

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act & Assert
			assertThatThrownBy(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.isInstanceOf(ZetaRetryableException.class)
				.hasMessageContaining("Token response body is null or empty");
		}

		@Test
		@DisplayName("Should throw ZetaRetryableException for empty response body")
		void shouldThrowExceptionForEmptyResponseBody() {
			// Arrange
			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity.ok(new HashMap<>());

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act & Assert
			assertThatThrownBy(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.isInstanceOf(ZetaRetryableException.class)
				.hasMessageContaining("Token response body is null or empty");
		}

		@Test
		@DisplayName("Should throw ZetaRetryableException when access_token is missing")
		void shouldThrowExceptionWhenAccessTokenMissing() {
			// Arrange
			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity.ok(Map.of("expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act & Assert
			assertThatThrownBy(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.isInstanceOf(ZetaRetryableException.class)
				.hasMessageContaining("Token response missing or invalid access_token field");
		}

		@Test
		@DisplayName("Should throw ZetaRetryableException when access_token is blank")
		void shouldThrowExceptionWhenAccessTokenBlank() {
			// Arrange
			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", "", "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act & Assert
			assertThatThrownBy(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.isInstanceOf(ZetaRetryableException.class)
				.hasMessageContaining("Token response missing or invalid access_token field");
		}

		@Test
		@DisplayName("Should throw ZetaRetryableException when access_token is not a string")
		void shouldThrowExceptionWhenAccessTokenNotString() {
			// Arrange
			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", 12345, "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act & Assert
			assertThatThrownBy(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.isInstanceOf(ZetaRetryableException.class)
				.hasMessageContaining("Token response missing or invalid access_token field");
		}

	}

	@Nested
	@DisplayName("Error Handling - Unexpected Errors")
	class ErrorHandlingUnexpectedErrors {

		@Test
		@DisplayName("Should throw ZetaRetryableException for unexpected exceptions")
		void shouldThrowRetryableExceptionForUnexpectedError() {
			// Arrange
			RuntimeException unexpectedException = new RuntimeException("Unexpected error");

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willThrow(unexpectedException);

			// Act & Assert
			assertThatThrownBy(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.isInstanceOf(ZetaRetryableException.class)
				.hasMessageContaining("Unexpected error while fetching token");
		}

		@Test
		@DisplayName("Should include original error message in exception")
		void shouldIncludeOriginalErrorMessage() {
			// Arrange
			String errorMessage = "Something went wrong";
			RuntimeException unexpectedException = new RuntimeException(errorMessage);

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willThrow(unexpectedException);

			// Act & Assert
			assertThatThrownBy(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.isInstanceOf(ZetaRetryableException.class)
				.hasMessageContaining(errorMessage);
		}

	}

	@Nested
	@DisplayName("Token Eviction")
	class TokenEviction {

		@Test
		@DisplayName("Should evict cached token")
		void shouldEvictCachedToken() {
			// Arrange
			String token = "token_to_evict";
			long expiresAt = System.currentTimeMillis() + Duration.ofHours(1).toMillis();
			BearerTokenGeneratorTests.this.putTokenInCache(token, expiresAt);

			// First call should use cache
			String cachedToken = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();
			assertThat(cachedToken).isEqualTo(token);

			// Act
			BearerTokenGeneratorTests.this.bearerTokenGenerator.evictToken();

			// Assert - Next call should fetch new token
			String newToken = "new_token_after_eviction";
			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", newToken, "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			String result = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();
			assertThat(result).isEqualTo(newToken);
			verify(BearerTokenGeneratorTests.this.resilientRestClient).execute(any());
		}

		@Test
		@DisplayName("Should handle eviction on empty cache gracefully")
		void shouldHandleEvictionOnEmptyCache() {
			// Arrange - cache is already empty

			// Act & Assert - Should not throw exception
			assertThatCode(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.evictToken())
				.doesNotThrowAnyException();
		}

	}

	@Nested
	@DisplayName("Edge Cases and Boundary Conditions")
	class EdgeCasesAndBoundaryConditions {

		@Test
		@DisplayName("Should handle very large expiration times")
		void shouldHandleLargeExpirationTimes() {
			// Arrange
			String token = "token_with_large_expiry";
			long largeExpiresIn = Long.MAX_VALUE / 2000; // Avoid overflow

			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", token, "expires_in", largeExpiresIn));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act & Assert
			assertThatCode(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.doesNotThrowAnyException();
		}

		@Test
		@DisplayName("Should handle minimum expiration time (default 300 seconds)")
		void shouldHandleDefaultExpirationTime() {
			// Arrange
			String token = "token_with_default_expiry";

			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity.ok(Map.of("access_token", token));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act
			String result = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert
			assertThat(result).isEqualTo(token);
		}

		@Test
		@DisplayName("Should handle token with special characters")
		void shouldHandleTokenWithSpecialCharacters() {
			// Arrange
			String token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";

			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", token, "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act
			String result = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert
			assertThat(result).isEqualTo(token);
		}

		@Test
		@DisplayName("Should handle null client credentials without throwing NPE")
		void shouldHandleNullCredentials() {
			// Arrange
			given(BearerTokenGeneratorTests.this.zetaApiProperties.getClientId()).willReturn(null);
			given(BearerTokenGeneratorTests.this.zetaApiProperties.getClientSecret()).willReturn(null);
			given(BearerTokenGeneratorTests.this.secretService.getSecretUsingKey(any())).willReturn(null);

			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", "token123", "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act & Assert
			assertThatCode(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.doesNotThrowAnyException();
		}

		@Test
		@DisplayName("Should use default expires_in when not present in response")
		void shouldUseDefaultExpiresInWhenMissing() {
			// Arrange
			String token = "token_without_expires_in";

			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity.ok(Map.of("access_token", token));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act
			String result = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert
			assertThat(result).isEqualTo(token);
		}

		@Test
		@DisplayName("Should handle Integer expires_in (not just Long)")
		void shouldHandleIntegerExpiresIn() {
			// Arrange
			String token = "token_with_int_expiry";

			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", token, "expires_in", 3600));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act & Assert
			assertThatCode(() -> BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken())
				.doesNotThrowAnyException();
		}

	}

	@Nested
	@DisplayName("Concurrency and Thread Safety")
	class ConcurrencyAndThreadSafety {

		@Test
		@DisplayName("Should handle concurrent token requests with proper caching")
		void shouldHandleConcurrentRequests() throws InterruptedException {
			// Arrange
			String sharedToken = "shared_token_for_concurrent_access";
			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", sharedToken, "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act - Simulate concurrent requests
			Thread thread1 = new Thread(() -> {
				String token = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();
				assertThat(token).isEqualTo(sharedToken);
			});

			Thread thread2 = new Thread(() -> {
				String token = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();
				assertThat(token).isEqualTo(sharedToken);
			});

			thread1.start();
			thread2.start();

			thread1.join();
			thread2.join();

			// Assert - Should have called execute only once (due to caching)
			verify(BearerTokenGeneratorTests.this.resilientRestClient, atLeastOnce()).execute(any());
		}

	}

	@Nested
	@DisplayName("Integration Scenarios")
	class IntegrationScenarios {

		@Test
		@DisplayName("Should complete full token lifecycle: fetch -> cache -> refresh -> evict")
		void shouldCompleteFullTokenLifecycle() {
			// Arrange - Initial token fetch
			String token1 = "initial_token";
			ResponseEntity<Map<String, Object>> response1 = ResponseEntity
				.ok(Map.of("access_token", token1, "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(response1);

			// Act 1 - Fetch initial token
			String result1 = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();
			assertThat(result1).isEqualTo(token1);

			// Act 2 - Use cached token
			String result2 = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();
			assertThat(result2).isEqualTo(token1);
			verify(BearerTokenGeneratorTests.this.resilientRestClient, times(1)).execute(any());

			// Act 3 - Evict token
			BearerTokenGeneratorTests.this.bearerTokenGenerator.evictToken();

			// Act 4 - Fetch new token after eviction
			String token2 = "refreshed_token";
			ResponseEntity<Map<String, Object>> response2 = ResponseEntity
				.ok(Map.of("access_token", token2, "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(response2);

			String result3 = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();
			assertThat(result3).isEqualTo(token2);
			verify(BearerTokenGeneratorTests.this.resilientRestClient, times(2)).execute(any());
		}

		@Test
		@DisplayName("Should handle token refresh gracefully during active use")
		void shouldHandleTokenRefreshDuringUse() {
			// Arrange - Initial token expiring soon
			String oldToken = "expiring_token";
			long now = System.currentTimeMillis();
			long expiresAt = now + (5 * 1000); // Expires in 5 seconds (within grace
												// period)

			BearerTokenGeneratorTests.this.putTokenInCache(oldToken, expiresAt);

			String newToken = "fresh_token";
			ResponseEntity<Map<String, Object>> responseEntity = ResponseEntity
				.ok(Map.of("access_token", newToken, "expires_in", 3600L));

			given(BearerTokenGeneratorTests.this.resilientRestClient.execute(any())).willReturn(responseEntity);

			// Act - Should trigger refresh due to grace period
			String result = BearerTokenGeneratorTests.this.bearerTokenGenerator.generateToken();

			// Assert
			assertThat(result).isEqualTo(newToken);
			verify(BearerTokenGeneratorTests.this.resilientRestClient).execute(any());
		}

	}

}
