package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.CommonConstants;
import com.uhg.optum.financials.payments.payer.dto.ResponseObject;
import com.uhg.optum.financials.payments.payer.util.BearerTokenGenerator;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatusCode;
import org.springframework.web.client.RestClientException;

import static org.assertj.core.api.AssertionsForClassTypes.assertThat;
import static org.mockito.BDDMockito.given;
import org.junit.jupiter.api.extension.ExtendWith;

@ExtendWith(MockitoExtension.class)
class RetryableApiServiceTests {

	@Mock
	private ResilientRestClientFactory resilientRestClientFactory;

	@Mock
	private BearerTokenGenerator thisBearerTokenGenerator;

	@Mock
	private ResilientRestClient resilientRestClient;

	private RetryableApiService<Object> thisService;

	private final Object thisPayload = new Object();

	private final String thisToken = "token123";

	private final String thisUrl = "http://test-url";

	@BeforeEach
	void setUp() {
		given(this.resilientRestClientFactory.getClient("zeta-api")).willReturn(this.resilientRestClient);
		this.thisService = new RetryableApiService<>(this.resilientRestClientFactory, this.thisBearerTokenGenerator);
	}

	@Test
	void callZetaApiWithRetry_success() {
		given(this.resilientRestClient.execute(org.mockito.ArgumentMatchers.any())).willReturn("ignored");
		ResponseObject response = this.thisService.callZetaApiWithRetry(this.thisPayload, this.thisToken, this.thisUrl);
		assertThat(response.getStatusCode()).isEqualTo(String.valueOf(CommonConstants.HTTP_202));
		assertThat(response.getMessage()).isEqualTo(CommonConstants.SUCCESS);
		assertThat(response.getHttpStatusCode()).isEqualTo(HttpStatusCode.valueOf(CommonConstants.HTTP_202));

	}

	@Test
	void callZetaApiWithRetry_restClientException_returns503ResponseObject() {
		given(this.resilientRestClient.execute(org.mockito.ArgumentMatchers.any()))
			.willThrow(new RestClientException("RestClient error"));

		ResponseObject response = this.thisService.callZetaApiWithRetry(this.thisPayload, this.thisToken, this.thisUrl);
		assertThat(response.getStatusCode()).isEqualTo(String.valueOf(CommonConstants.HTTP_503));
		assertThat(response.getMessage()).contains("Connection error after retries");
		assertThat(response.getHttpStatusCode()).isEqualTo(HttpStatusCode.valueOf(CommonConstants.HTTP_503));
	}

	@Test
	void callZetaApiWithRetry_executeThrowsResponseObjectIsReturned() {
		given(this.resilientRestClient.execute(org.mockito.ArgumentMatchers.any()))
			.willThrow(new RestClientException("RestClient error"));

		ResponseObject response = this.thisService.callZetaApiWithRetry(this.thisPayload, this.thisToken, this.thisUrl);
		assertThat(response.getStatusCode()).isEqualTo(String.valueOf(CommonConstants.HTTP_503));
		assertThat(response.getMessage()).contains("RestClient error");
	}

	@Test
	void callZetaApiWithRetry_readMethod_returnsResponseEntity() {
		given(this.resilientRestClient.execute(org.mockito.ArgumentMatchers.any())).willReturn("payload");

		Object response = this.thisService.callZetaApiWithRetry(this.thisPayload, this.thisToken, this.thisUrl, "GET",
				String.class);
		assertThat(response).isInstanceOf(org.springframework.http.ResponseEntity.class);
	}

}
