package com.uhg.optum.financials.payments.payer.service.impl;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentCallbackDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentCallbackHeaderDto;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.core.env.Environment;
import org.springframework.http.ResponseEntity;

import java.lang.reflect.Field;
import java.util.Map;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.Mockito.never;

@ExtendWith(MockitoExtension.class)
class StpEnrollmentOutboundClientImplTests {

	@Mock
	private ResilientRestClientFactory resilientRestClientFactory;

	@Mock
	private ResilientRestClient callbackClient;

	@Mock
	private ResilientRestClient tokenClient;

	@Mock
	private LogUtility logUtility;

	@Mock
	private Environment environment;

	@Mock
	private SecretService secretService;

	private StpEnrollmentOutboundClientImpl client;

	@BeforeEach
	void setUp() {
		given(this.resilientRestClientFactory.getClient("stp-enrollment")).willReturn(this.callbackClient);
		given(this.resilientRestClientFactory.getClient("stp-enrollment-token")).willReturn(this.tokenClient);
		this.client = new StpEnrollmentOutboundClientImpl(this.resilientRestClientFactory, new ObjectMapper(),
				this.logUtility, this.environment, this.secretService);
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
	void sendValidationCallback_withOAuthConfig_addsBearerHeader() {
		String endpoint = "https://callback/stp-partner123";
		String providerPrefix = "stp.enrollment.providers.stp-partner123.";
		given(this.environment.getProperty(providerPrefix + "auth-type")).willReturn("oauth");
		given(this.environment.getProperty(providerPrefix + "oauth.token-url")).willReturn("https://oauth/token");
		given(this.environment.getProperty(providerPrefix + "oauth.scope")).willReturn("stp.scope");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-stp-partner123-oauth-client-id"))
			.willReturn("cid");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-stp-partner123-oauth-client-secret"))
			.willReturn("secret");
		given(this.tokenClient.execute(any()))
			.willReturn(ResponseEntity.ok("{\"access_token\":\"token-123\",\"expires_in\":300}"));
		given(this.callbackClient.execute(any())).willReturn(ResponseEntity.ok("ok"));

		boolean success = this.client.sendValidationCallback(buildCallback("STP-Partner123"), endpoint);

		assertThat(success).isTrue();
		then(this.callbackClient).should().execute(any());
	}

	@Test
	void sendValidationCallback_withoutOAuthConfig_doesNotFetchToken() {
		String endpoint = "https://callback/stp-partner123";
		String providerPrefix = "stp.enrollment.providers.stp-partner123.";
		given(this.environment.getProperty(providerPrefix + "auth-type")).willReturn("none");
		given(this.callbackClient.execute(any())).willReturn(ResponseEntity.ok("ok"));

		boolean success = this.client.sendValidationCallback(buildCallback("STP-Partner123"), endpoint);

		assertThat(success).isTrue();
		then(this.tokenClient).should(never()).execute(any());
	}

	@Test
	void sendValidationCallback_whenVaultSecretMissing_usesDefaultCredentialsAndStillFetchesToken() {
		String endpoint = "https://callback/stp-partner123";
		String providerPrefix = "stp.enrollment.providers.stp-partner123.";
		given(this.environment.getProperty(providerPrefix + "auth-type")).willReturn("oauth");
		given(this.environment.getProperty(providerPrefix + "oauth.token-url")).willReturn("https://oauth/token");
		given(this.environment.getProperty(providerPrefix + "oauth.scope")).willReturn("stp.scope");
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-stp-partner123-oauth-client-id"))
			.willReturn(null);
		given(this.secretService.getSecretUsingKey("stp-enrollment-providers-stp-partner123-oauth-client-secret"))
			.willReturn(null);
		given(this.tokenClient.execute(any()))
			.willReturn(ResponseEntity.ok("{\"access_token\":\"token-123\",\"expires_in\":300}"));
		given(this.callbackClient.execute(any())).willReturn(ResponseEntity.ok("ok"));

		boolean success = this.client.sendValidationCallback(buildCallback("STP-Partner123"), endpoint);

		assertThat(success).isTrue();
		// Missing client-id/secret in the vault now falls back to
		// StpOAuthHelper.DEFAULT_CLIENT_ID/DEFAULT_CLIENT_SECRET instead of sending a
		// blank credential, so the token fetch still succeeds using the default values.
		then(this.tokenClient).should().execute(any());
		then(this.callbackClient).should().execute(any());
	}

	private StpEnrollmentCallbackDto buildCallback(String networkPartner) {
		StpEnrollmentCallbackHeaderDto header = new StpEnrollmentCallbackHeaderDto();
		header.setNetworkPartner(networkPartner);
		header.setCorrelationId("corr-1");
		header.setStatus("Error");

		StpEnrollmentCallbackDto dto = new StpEnrollmentCallbackDto();
		dto.setHeader(header);
		return dto;
	}

}
