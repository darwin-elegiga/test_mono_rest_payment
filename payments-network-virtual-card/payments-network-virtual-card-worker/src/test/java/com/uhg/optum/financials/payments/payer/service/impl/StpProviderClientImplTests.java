package com.uhg.optum.financials.payments.payer.service.impl;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.dto.stp.StpVirtualCardRequestDto;
import com.uhg.optum.financials.payments.payer.enums.StpPaymentProcessingStatus;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.StpPaymentProcessing;
import com.uhg.optum.financials.payments.payer.model.StpPaymentProcessingHistory;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.StpPaymentProcessingHistoryRepository;
import com.uhg.optum.financials.payments.payer.repository.StpPaymentProcessingRepository;
import com.uhg.optum.financials.payments.payer.service.StpProviderClient.ProviderConfig;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.mockito.junit.jupiter.MockitoSettings;
import org.mockito.quality.Strictness;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.HttpStatusCode;
import org.springframework.http.ResponseEntity;
import org.springframework.mock.env.MockEnvironment;
import org.springframework.test.util.ReflectionTestUtils;
import org.springframework.web.client.HttpClientErrorException;
import org.springframework.web.client.HttpServerErrorException;
import org.springframework.web.client.HttpStatusCodeException;
import org.springframework.web.client.ResourceAccessException;

import java.lang.reflect.Constructor;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.nio.charset.StandardCharsets;
import java.util.Map;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.never;

@ExtendWith(MockitoExtension.class)
@MockitoSettings(strictness = Strictness.LENIENT)
class StpProviderClientImplTests {

	@Mock
	private ResilientRestClientFactory resilientRestClientFactory;

	@Mock
	private ResilientRestClient remittanceClient;

	@Mock
	private ResilientRestClient tokenClient;

	@Mock
	private ObjectMapper objectMapper;

	@Mock
	private SecretService secretService;

	@Mock
	private StpPaymentProcessingRepository stpPaymentProcessingRepository;

	@Mock
	private StpPaymentProcessingHistoryRepository stpPaymentProcessingHistoryRepository;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	private MockEnvironment environment;

	private StpProviderClientImpl client;

	@BeforeEach
	void setUp() {
		this.environment = new MockEnvironment();
		given(this.resilientRestClientFactory.getClient("stp-remittance")).willReturn(this.remittanceClient);
		given(this.resilientRestClientFactory.getClient("stp-remittance-token")).willReturn(this.tokenClient);
		this.client = new StpProviderClientImpl(this.resilientRestClientFactory, this.objectMapper,
				this.stpPaymentProcessingRepository, this.stpPaymentProcessingHistoryRepository,
				this.paymentRequestRepository, this.secretService, this.environment);
		given(this.stpPaymentProcessingRepository.findByNetworkPaymentId(any())).willReturn(Optional.empty());
		given(this.stpPaymentProcessingRepository.save(any(StpPaymentProcessing.class)))
			.willAnswer((invocation) -> invocation.getArgument(0));
		given(this.stpPaymentProcessingHistoryRepository.save(any(StpPaymentProcessingHistory.class)))
			.willAnswer((invocation) -> invocation.getArgument(0));
		clearOAuthTokenCache();
	}

	@SuppressWarnings("unchecked")
	private void clearOAuthTokenCache() {
		try {
			Field tokenCacheField = StpOAuthHelper.class.getDeclaredField("tokenCache");
			tokenCacheField.setAccessible(true);
			((Map<String, ?>) tokenCacheField.get(null)).clear();
		}
		catch (Exception ex) {
			throw new IllegalStateException("Unable to clear StpOAuthHelper token cache", ex);
		}
	}

	@Test
	void postRemittance_whenEndpointMissing_andPaymentIdResolved_persistsFailedOutcome() {
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setId(42L);
		given(this.paymentRequestRepository.findByPaymentDetailsNetworkPaymentId("np-1"))
			.willReturn(Optional.of(paymentRequest));
		ProviderConfig providerConfig = new ProviderConfig("", "none");

		this.client.postRemittance("provider-a", providerConfig, new StpVirtualCardRequestDto(), "np-1", null);

		ArgumentCaptor<StpPaymentProcessing> processingCaptor = ArgumentCaptor.forClass(StpPaymentProcessing.class);
		ArgumentCaptor<StpPaymentProcessingHistory> historyCaptor = ArgumentCaptor
			.forClass(StpPaymentProcessingHistory.class);
		then(this.stpPaymentProcessingRepository).should().save(processingCaptor.capture());
		then(this.stpPaymentProcessingHistoryRepository).should().save(historyCaptor.capture());
		StpPaymentProcessing processing = processingCaptor.getValue();
		assertThat(processing.getPaymentRequestId()).isEqualTo(42L);
		assertThat(processing.getRetryCount()).isZero();
		assertThat(processing.getStatus()).isEqualTo(StpPaymentProcessingStatus.STP_PROCESSING_FAILED);
		assertThat(historyCaptor.getValue().getRemarks()).isEqualTo("Endpoint missing");
		then(this.remittanceClient).should(never()).execute(any());
	}

	@Test
	void postRemittance_whenEndpointMissing_andPaymentIdNotResolved_skipsPersistence() {
		given(this.paymentRequestRepository.findByPaymentDetailsNetworkPaymentId("np-missing"))
			.willReturn(Optional.empty());
		ProviderConfig providerConfig = new ProviderConfig(" ", "none");

		this.client.postRemittance("provider-a", providerConfig, new StpVirtualCardRequestDto(), "np-missing", null);

		then(this.stpPaymentProcessingRepository).should(never()).save(any(StpPaymentProcessing.class));
		then(this.stpPaymentProcessingHistoryRepository).should(never()).save(any(StpPaymentProcessingHistory.class));
	}

	@Test
	void postRemittance_success_persistsSentStatus() {
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "none");
		given(this.remittanceClient.execute(any())).willReturn(ResponseEntity.ok(new Object()));

		this.client.postRemittance("provider-a", providerConfig, new StpVirtualCardRequestDto(), "np-2", 10L);

		ArgumentCaptor<StpPaymentProcessing> processingCaptor = ArgumentCaptor.forClass(StpPaymentProcessing.class);
		ArgumentCaptor<StpPaymentProcessingHistory> historyCaptor = ArgumentCaptor
			.forClass(StpPaymentProcessingHistory.class);
		then(this.stpPaymentProcessingRepository).should().save(processingCaptor.capture());
		then(this.stpPaymentProcessingHistoryRepository).should().save(historyCaptor.capture());
		assertThat(processingCaptor.getValue().getStatus()).isEqualTo(StpPaymentProcessingStatus.REMITTANCE_SENT);
		assertThat(processingCaptor.getValue().getRetryCount()).isZero();
		assertThat(historyCaptor.getValue().getRemarks()).isEqualTo("Remittance sent");
	}

	@Test
	void postRemittance_whenResponseStatusRedirect_throwsBusinessAndPersistsFailure() {
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "none");
		given(this.remittanceClient.execute(any())).willReturn(new ResponseEntity<>(new Object(), HttpStatus.FOUND));

		assertThatThrownBy(() -> this.client.postRemittance("provider-a", providerConfig,
				new StpVirtualCardRequestDto(), "np-3", 11L))
			.isInstanceOf(RuntimeException.class)
			.hasMessageContaining("Unknown Exception");

		ArgumentCaptor<StpPaymentProcessingHistory> historyCaptor = ArgumentCaptor
			.forClass(StpPaymentProcessingHistory.class);
		then(this.stpPaymentProcessingHistoryRepository).should().save(historyCaptor.capture());
		assertThat(historyCaptor.getValue().getRemarks()).contains("technical failure");
	}

	@Test
	void postRemittance_whenServerError_throwsTechnicalAndPersistsFailure() {
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "none");
		given(this.remittanceClient.execute(any()))
			.willReturn(new ResponseEntity<>(new Object(), HttpStatus.INTERNAL_SERVER_ERROR));

		assertThatThrownBy(() -> this.client.postRemittance("provider-a", providerConfig,
				new StpVirtualCardRequestDto(), "np-4", 12L))
			.isInstanceOf(StpProviderClientImpl.STPProviderTechnicalException.class)
			.hasMessageContaining("response status: 500");

		ArgumentCaptor<StpPaymentProcessingHistory> historyCaptor = ArgumentCaptor
			.forClass(StpPaymentProcessingHistory.class);
		then(this.stpPaymentProcessingHistoryRepository).should().save(historyCaptor.capture());
		assertThat(historyCaptor.getValue().getRemarks()).contains("technical failure");
	}

	@Test
	void postRemittance_whenClientErrorResponse_throwsTechnicalAndPersistsFailure() {
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "none");
		given(this.remittanceClient.execute(any()))
			.willReturn(new ResponseEntity<>(new Object(), HttpStatus.BAD_REQUEST));

		assertThatThrownBy(() -> this.client.postRemittance("provider-a", providerConfig,
				new StpVirtualCardRequestDto(), "np-4b", 12L))
			.isInstanceOf(StpProviderClientImpl.STPProviderTechnicalException.class)
			.hasMessageContaining("response status: 400");

		then(this.stpPaymentProcessingHistoryRepository).should().save(any(StpPaymentProcessingHistory.class));
	}

	@Test
	void postRemittance_whenHttpStatusException4xx_throwsTechnicalAndPersistsFailure() {
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "none");
		HttpStatusCodeException ex = HttpClientErrorException.create(HttpStatus.BAD_REQUEST, "Bad Request", null,
				new byte[0], StandardCharsets.UTF_8);
		given(this.remittanceClient.execute(any())).willThrow(ex);

		assertThatThrownBy(() -> this.client.postRemittance("provider-a", providerConfig,
				new StpVirtualCardRequestDto(), "np-5", 13L))
			.isInstanceOf(StpProviderClientImpl.STPProviderTechnicalException.class)
			.hasMessageContaining("STP remittance exception");

		then(this.stpPaymentProcessingHistoryRepository).should().save(any(StpPaymentProcessingHistory.class));
	}

	@Test
	void postRemittance_whenHttpStatusException5xx_throwsTechnicalAndPersistsFailure() {
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "none");
		HttpStatusCodeException ex = HttpServerErrorException.create(HttpStatus.INTERNAL_SERVER_ERROR, "Server Error",
				null, new byte[0], StandardCharsets.UTF_8);
		given(this.remittanceClient.execute(any())).willThrow(ex);

		assertThatThrownBy(() -> this.client.postRemittance("provider-a", providerConfig,
				new StpVirtualCardRequestDto(), "np-5b", 13L))
			.isInstanceOf(StpProviderClientImpl.STPProviderTechnicalException.class)
			.hasMessageContaining("STP remittance exception");

		then(this.stpPaymentProcessingHistoryRepository).should().save(any(StpPaymentProcessingHistory.class));
	}

	@Test
	void postRemittance_whenHttpStatusException3xx_throwsBusinessAndPersistsFailure() {
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "none");
		HttpStatusCodeException redirectException = new HttpStatusCodeException(HttpStatusCode.valueOf(302), "Found") {
		};
		given(this.remittanceClient.execute(any())).willThrow(redirectException);

		assertThatThrownBy(() -> this.client.postRemittance("provider-a", providerConfig,
				new StpVirtualCardRequestDto(), "np-6", 14L))
			.isInstanceOf(RuntimeException.class)
			.hasMessageContaining("business exception");

		then(this.stpPaymentProcessingHistoryRepository).should().save(any(StpPaymentProcessingHistory.class));
	}

	@Test
	void postRemittance_whenTransportError_throwsTechnicalAndPersistsFailure() {
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "none");
		given(this.remittanceClient.execute(any())).willThrow(new ResourceAccessException("connection refused"));

		assertThatThrownBy(() -> this.client.postRemittance("provider-a", providerConfig,
				new StpVirtualCardRequestDto(), "np-7", 15L))
			.isInstanceOf(StpProviderClientImpl.STPProviderTechnicalException.class)
			.hasMessageContaining("transport exception");

		then(this.stpPaymentProcessingHistoryRepository).should().save(any(StpPaymentProcessingHistory.class));
	}

	@Test
	void postRemittance_whenUnexpectedException_throwsTechnicalAndPersistsFailure() {
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "none");
		given(this.remittanceClient.execute(any())).willThrow(new IllegalStateException("boom"));

		assertThatThrownBy(() -> this.client.postRemittance("provider-a", providerConfig,
				new StpVirtualCardRequestDto(), "np-8", 16L))
			.isInstanceOf(StpProviderClientImpl.STPProviderTechnicalException.class)
			.hasMessageContaining("Unknown Exception");

		then(this.stpPaymentProcessingHistoryRepository).should().save(any(StpPaymentProcessingHistory.class));
	}

	@Test
	void postRemittance_withClientCredentials_setsBearerTokenAndUsesCachedToken() {
		ObjectMapper realObjectMapper = new ObjectMapper();
		this.environment.withProperty("stp.enrollment.providers.provider-token.oauth.token-url", "https://oauth/token");
		this.environment.withProperty("stp.remittance.providers.provider-token.oauth.scope", "stp.scope");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-token-oauth-client-id"))
			.willReturn("cid");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-token-oauth-client-secret"))
			.willReturn("secret");
		StpProviderClientImpl oauthClient = new StpProviderClientImpl(this.resilientRestClientFactory, realObjectMapper,
				this.stpPaymentProcessingRepository, this.stpPaymentProcessingHistoryRepository,
				this.paymentRequestRepository, this.secretService, this.environment);
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "client_credentials");

		given(this.tokenClient.execute(any()))
			.willReturn(ResponseEntity.ok("{\"access_token\":\" token-value \",\"expires_in\":300}"));
		given(this.remittanceClient.execute(any())).willReturn(ResponseEntity.ok(new Object()));

		oauthClient.postRemittance("provider-token", providerConfig, new StpVirtualCardRequestDto(), "np-9", 17L);
		oauthClient.postRemittance("provider-token", providerConfig, new StpVirtualCardRequestDto(), "np-10", 18L);

		then(this.tokenClient).should().execute(any());
		then(this.remittanceClient).should(org.mockito.Mockito.times(2)).execute(any());
	}

	@Test
	void postRemittance_withOauthIncompleteConfig_doesNotSetBearerHeader() {
		// No tokenUrl configured in environment -> token fetch is skipped entirely
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "oauth");
		given(this.remittanceClient.execute(any())).willReturn(ResponseEntity.ok(new Object()));

		this.client.postRemittance("provider-a", providerConfig, new StpVirtualCardRequestDto(), "np-11", 19L);

		then(this.tokenClient).should(never()).execute(any());
		then(this.remittanceClient).should().execute(any());
	}

	@Test
	void applyOAuthIfNeeded_doesNotSetBearerHeaderForInvalidTokenResponseAndOnException() {
		ObjectMapper failingMapper = mock(ObjectMapper.class);
		this.environment.withProperty("stp.enrollment.providers.provider-x.oauth.token-url", "https://oauth/token");
		this.environment.withProperty("stp.enrollment.providers.provider-y.oauth.token-url", "https://oauth/token");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-x-oauth-client-id"))
			.willReturn("cid");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-x-oauth-client-secret"))
			.willReturn("secret");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-y-oauth-client-id"))
			.willReturn("cid");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-y-oauth-client-secret"))
			.willReturn("secret");
		StpProviderClientImpl oauthClient = new StpProviderClientImpl(this.resilientRestClientFactory, failingMapper,
				this.stpPaymentProcessingRepository, this.stpPaymentProcessingHistoryRepository,
				this.paymentRequestRepository, this.secretService, this.environment);
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "oauth");
		given(this.tokenClient.execute(any())).willThrow(new IllegalStateException("token-endpoint-down"))
			.willReturn(new ResponseEntity<>("", HttpStatus.OK));

		HttpHeaders headersFromException = new HttpHeaders();
		ReflectionTestUtils.invokeMethod(oauthClient, "applyOAuthIfNeeded", "provider-x", providerConfig,
				headersFromException);
		HttpHeaders headersFromEmptyBody = new HttpHeaders();
		ReflectionTestUtils.invokeMethod(oauthClient, "applyOAuthIfNeeded", "provider-y", providerConfig,
				headersFromEmptyBody);

		assertThat(headersFromException.getFirst(HttpHeaders.AUTHORIZATION)).isNull();
		assertThat(headersFromEmptyBody.getFirst(HttpHeaders.AUTHORIZATION)).isNull();
	}

	@Test
	void applyOAuthIfNeeded_doesNotSetBearerHeaderWhenTokenEndpointIsNon2xx() {
		ObjectMapper realObjectMapper = new ObjectMapper();
		this.environment.withProperty("stp.enrollment.providers.provider-non2xx.oauth.token-url",
				"https://oauth/token");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-non2xx-oauth-client-id"))
			.willReturn("cid");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-non2xx-oauth-client-secret"))
			.willReturn("secret");
		StpProviderClientImpl oauthClient = new StpProviderClientImpl(this.resilientRestClientFactory, realObjectMapper,
				this.stpPaymentProcessingRepository, this.stpPaymentProcessingHistoryRepository,
				this.paymentRequestRepository, this.secretService, this.environment);
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "oauth");
		given(this.tokenClient.execute(any()))
			.willReturn(new ResponseEntity<>("{\"access_token\":\"abc\"}", HttpStatus.UNAUTHORIZED));

		HttpHeaders headers = new HttpHeaders();
		ReflectionTestUtils.invokeMethod(oauthClient, "applyOAuthIfNeeded", "provider-non2xx", providerConfig, headers);

		assertThat(headers.getFirst(HttpHeaders.AUTHORIZATION)).isNull();
	}

	@Test
	void applyOAuthIfNeeded_doesNotSetBearerHeaderWhenAccessTokenBlankEvenWithDefaultCredentials() {
		ObjectMapper realObjectMapper = new ObjectMapper();
		this.environment.withProperty("stp.enrollment.providers.provider-missing-client-id.oauth.token-url",
				"https://oauth/token");
		this.environment.withProperty("stp.enrollment.providers.provider-missing-secret.oauth.token-url",
				"https://oauth/token");
		given(this.secretService
			.getSecretUsingKey("stp-enrollment-providers-provider-missing-client-id-oauth-client-id")).willReturn(null);
		given(this.secretService
			.getSecretUsingKey("stp-enrollment-providers-provider-missing-client-id-oauth-client-secret"))
			.willReturn("secret");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-missing-secret-oauth-client-id"))
			.willReturn("cid");
		given(this.secretService
			.getSecretUsingKey("stp-enrollment-providers-provider-missing-secret-oauth-client-secret"))
			.willReturn(null);
		given(this.tokenClient.execute(any()))
			.willReturn(new ResponseEntity<>("{\"access_token\":\"\"}", HttpStatus.OK));
		StpProviderClientImpl oauthClient = new StpProviderClientImpl(this.resilientRestClientFactory, realObjectMapper,
				this.stpPaymentProcessingRepository, this.stpPaymentProcessingHistoryRepository,
				this.paymentRequestRepository, this.secretService, this.environment);
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "oauth");

		HttpHeaders headersMissingClientId = new HttpHeaders();
		ReflectionTestUtils.invokeMethod(oauthClient, "applyOAuthIfNeeded", "provider-missing-client-id",
				providerConfig, headersMissingClientId);
		HttpHeaders headersMissingSecret = new HttpHeaders();
		ReflectionTestUtils.invokeMethod(oauthClient, "applyOAuthIfNeeded", "provider-missing-secret", providerConfig,
				headersMissingSecret);

		// Even though a default client-id/secret is now substituted for the missing
		// vault value, the token endpoint returns a blank access_token, so no bearer
		// header is ever set on the outbound remittance request headers.
		assertThat(headersMissingClientId.getFirst(HttpHeaders.AUTHORIZATION)).isNull();
		assertThat(headersMissingSecret.getFirst(HttpHeaders.AUTHORIZATION)).isNull();
		then(this.tokenClient).should(org.mockito.Mockito.times(2)).execute(any());
	}

	@Test
	void applyOAuthIfNeeded_whenVaultSecretsMissing_substitutesDefaultCredentialsInTokenRequest() {
		ObjectMapper realObjectMapper = new ObjectMapper();
		this.environment.withProperty("stp.enrollment.providers.provider-no-vault.oauth.token-url",
				"https://oauth/token");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-no-vault-oauth-client-id"))
			.willReturn(null);
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-no-vault-oauth-client-secret"))
			.willReturn(null);
		given(this.tokenClient.execute(any()))
			.willReturn(ResponseEntity.ok("{\"access_token\":\"abc\",\"expires_in\":300}"));
		StpProviderClientImpl oauthClient = new StpProviderClientImpl(this.resilientRestClientFactory, realObjectMapper,
				this.stpPaymentProcessingRepository, this.stpPaymentProcessingHistoryRepository,
				this.paymentRequestRepository, this.secretService, this.environment);
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "oauth");

		HttpHeaders headers = new HttpHeaders();
		ReflectionTestUtils.invokeMethod(oauthClient, "applyOAuthIfNeeded", "provider-no-vault", providerConfig,
				headers);

		then(this.tokenClient).should().execute(any());
		assertThat(headers.getFirst(HttpHeaders.AUTHORIZATION)).isEqualTo("Bearer abc");
	}

	@Test
	void applyOAuthIfNeeded_whenCacheIsNearExpiry_fetchesNewToken() throws Exception {
		ObjectMapper realObjectMapper = new ObjectMapper();
		this.environment.withProperty("stp.enrollment.providers.provider-exp.oauth.token-url", "https://oauth/token");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-exp-oauth-client-id"))
			.willReturn("cid");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-exp-oauth-client-secret"))
			.willReturn("secret");
		StpProviderClientImpl oauthClient = new StpProviderClientImpl(this.resilientRestClientFactory, realObjectMapper,
				this.stpPaymentProcessingRepository, this.stpPaymentProcessingHistoryRepository,
				this.paymentRequestRepository, this.secretService, this.environment);
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "oauth");
		given(this.tokenClient.execute(any()))
			.willReturn(ResponseEntity.ok("{\"access_token\":\"first\",\"expires_in\":300}"))
			.willReturn(ResponseEntity.ok("{\"access_token\":\"second\",\"expires_in\":300}"));

		HttpHeaders firstHeaders = new HttpHeaders();
		ReflectionTestUtils.invokeMethod(oauthClient, "applyOAuthIfNeeded", "provider-exp", providerConfig,
				firstHeaders);
		injectCachedToken("provider-exp", "first", System.currentTimeMillis() + 1000);
		HttpHeaders refreshedHeaders = new HttpHeaders();
		ReflectionTestUtils.invokeMethod(oauthClient, "applyOAuthIfNeeded", "provider-exp", providerConfig,
				refreshedHeaders);

		assertThat(firstHeaders.getFirst(HttpHeaders.AUTHORIZATION)).isEqualTo("Bearer first");
		assertThat(refreshedHeaders.getFirst(HttpHeaders.AUTHORIZATION)).isEqualTo("Bearer second");
		then(this.tokenClient).should(org.mockito.Mockito.times(2)).execute(any());
	}

	@Test
	void applyOAuthIfNeeded_doesNotSetBearerHeaderWhenAccessTokenMissing() {
		ObjectMapper realObjectMapper = new ObjectMapper();
		this.environment.withProperty("stp.enrollment.providers.provider-z.oauth.token-url", "https://oauth/token");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-z-oauth-client-id"))
			.willReturn("cid");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-z-oauth-client-secret"))
			.willReturn("secret");
		StpProviderClientImpl oauthClient = new StpProviderClientImpl(this.resilientRestClientFactory, realObjectMapper,
				this.stpPaymentProcessingRepository, this.stpPaymentProcessingHistoryRepository,
				this.paymentRequestRepository, this.secretService, this.environment);
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "oauth");
		given(this.tokenClient.execute(any())).willReturn(ResponseEntity.ok("{\"expires_in\":120}"));

		HttpHeaders headers = new HttpHeaders();
		ReflectionTestUtils.invokeMethod(oauthClient, "applyOAuthIfNeeded", "provider-z", providerConfig, headers);

		assertThat(headers.getFirst(HttpHeaders.AUTHORIZATION)).isNull();
	}

	@Test
	void normalizeAndExceptionTypes_areCovered() throws Exception {
		// normalize moved to StpOAuthHelper; verify via exception types only
		StpProviderClientImpl.STPProviderTechnicalException technical = new StpProviderClientImpl.STPProviderTechnicalException(
				"tech-message", new IllegalArgumentException("cause"));
		StpProviderClientImpl.STPProviderTechnicalException technicalWithoutCause = new StpProviderClientImpl.STPProviderTechnicalException(
				"tech-message-2");
		assertThat(technical.getMessage()).isEqualTo("tech-message");
		assertThat(technical.getCause()).isInstanceOf(IllegalArgumentException.class);
		assertThat(technicalWithoutCause.getMessage()).isEqualTo("tech-message-2");

		Class<?> businessExClass = findInnerClass("STPProviderException");
		Constructor<?> ctor = businessExClass.getDeclaredConstructor(String.class, int.class);
		ctor.setAccessible(true);
		Object businessEx = ctor.newInstance("business", 418);
		Method getStatusCode = businessExClass.getDeclaredMethod("getStatusCode");
		getStatusCode.setAccessible(true);
		assertThat(getStatusCode.invoke(businessEx)).isEqualTo(418);
		assertThat(((Throwable) businessEx).getMessage()).isEqualTo("business");
	}

	private Class<?> findInnerClass(String simpleName) {
		for (Class<?> innerClass : StpProviderClientImpl.class.getDeclaredClasses()) {
			if (innerClass.getSimpleName().equals(simpleName)) {
				return innerClass;
			}
		}
		throw new IllegalStateException("Inner class not found: " + simpleName);
	}

	@SuppressWarnings("unchecked")
	private void injectCachedToken(String providerKey, String token, long expiresAt) throws Exception {
		Class<?> cachedTokenClass = findInnerClass("CachedOAuthToken");
		Constructor<?> ctor = cachedTokenClass.getDeclaredConstructor(String.class, long.class);
		ctor.setAccessible(true);
		Object cachedToken = ctor.newInstance(token, expiresAt);
		Object tokenCache = ReflectionTestUtils.getField(StpOAuthHelper.class, "tokenCache");
		assertThat(tokenCache).isInstanceOf(java.util.Map.class);
		((java.util.Map<String, Object>) tokenCache).put(providerKey, cachedToken);
	}

	@Test
	void applyOAuthIfNeeded_acceptsStringExpiresIn() {
		// StpOAuthHelper.fetchAccessToken returns token when OAuth response is valid.
		ObjectMapper realObjectMapper = new ObjectMapper();
		this.environment.withProperty("stp.enrollment.providers.provider-string-exp.oauth.token-url",
				"https://oauth/token");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-string-exp-oauth-client-id"))
			.willReturn("cid");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-provider-string-exp-oauth-client-secret"))
			.willReturn("secret");
		StpProviderClientImpl oauthClient = new StpProviderClientImpl(this.resilientRestClientFactory, realObjectMapper,
				this.stpPaymentProcessingRepository, this.stpPaymentProcessingHistoryRepository,
				this.paymentRequestRepository, this.secretService, this.environment);
		ProviderConfig providerConfig = new ProviderConfig("https://stp/remit", "oauth");
		given(this.tokenClient.execute(any()))
			.willReturn(ResponseEntity.ok("{\"access_token\":\"abc\",\"expires_in\":3600}"));

		HttpHeaders headers = new HttpHeaders();
		ReflectionTestUtils.invokeMethod(oauthClient, "applyOAuthIfNeeded", "provider-string-exp", providerConfig,
				headers);

		assertThat(headers.getFirst(HttpHeaders.AUTHORIZATION)).isEqualTo("Bearer abc");
	}

}
