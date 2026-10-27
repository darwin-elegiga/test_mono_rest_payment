package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.CommonConstants;
import com.uhg.optum.financials.payments.payer.dto.ResponseObject;
import com.uhg.optum.financials.payments.payer.util.BearerTokenGenerator;
import com.uhg.optum.financials.shared.core.http.resilience.DownstreamUnavailableException;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.HttpStatusCode;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestClient;
import org.springframework.web.client.RestClientException;
import org.springframework.web.client.RestClientResponseException;

@Service
@Slf4j
public class RetryableApiService<T> {

	private final ResilientRestClientFactory resilientRestClientFactory;

	private final BearerTokenGenerator bearerTokenGenerator;

	public RetryableApiService(ResilientRestClientFactory resilientRestClientFactory,
			BearerTokenGenerator bearerTokenGenerator) {
		this.resilientRestClientFactory = resilientRestClientFactory;
		this.bearerTokenGenerator = bearerTokenGenerator;
	}

	public ResponseObject callZetaApiWithRetry(T t, String token, String url) {
		return (ResponseObject) callZetaApiWithRetry("zeta-api", t, token, url, "POST", ResponseObject.class);
	}

	public Object callZetaApiWithRetry(T t, String token, String url, String httpMethod, Class<?> responseType) {
		return callZetaApiWithRetry("zeta-api", t, token, url, httpMethod, responseType);
	}

	public Object callZetaApiWithRetry(String clientName, T t, String token, String url, String httpMethod,
			Class<?> responseType) {
		try {
			ResilientRestClient resilientRestClient = this.resilientRestClientFactory.getClient(clientName);
			Object response = resilientRestClient.execute((restClient) -> {
				RestClient.RequestBodySpec requestSpec = createRequestSpec(restClient, httpMethod, url);
				requestSpec.header(HttpHeaders.CONTENT_TYPE, MediaType.APPLICATION_JSON_VALUE)
					.header(HttpHeaders.AUTHORIZATION, "Bearer " + token);
				if (!isReadMethod(httpMethod)) {
					requestSpec.body(t);
				}
				return requestSpec.retrieve().body(responseType);
			});
			if (isReadMethod(httpMethod)) {
				return ResponseEntity.status(HttpStatus.OK).body(response);
			}
			return new ResponseObject(String.valueOf(CommonConstants.HTTP_202), CommonConstants.SUCCESS, 0,
					HttpStatusCode.valueOf(CommonConstants.HTTP_202));
		}
		catch (RestClientResponseException ex) {
			String errorMsg = ex.getResponseBodyAsString();
			int statusCode = ex.getStatusCode().value();
			if (HttpStatus.FORBIDDEN.equals(ex.getStatusCode()) || HttpStatus.UNAUTHORIZED.equals(ex.getStatusCode())) {
				this.bearerTokenGenerator.evictToken();
			}
			if (isReadMethod(httpMethod)) {
				return ResponseEntity.status(statusCode).body(errorMsg);
			}
			return new ResponseObject(String.valueOf(statusCode), CommonConstants.ERROR + errorMsg, 0,
					HttpStatusCode.valueOf(statusCode));
		}
		catch (DownstreamUnavailableException | RestClientException ex) {
			String errorMsg = ex.getMessage();
			if (isReadMethod(httpMethod)) {
				return ResponseEntity.status(HttpStatus.SERVICE_UNAVAILABLE).body(errorMsg);
			}
			return new ResponseObject(String.valueOf(CommonConstants.HTTP_503),
					"Connection error after retries: " + errorMsg, 0, HttpStatus.SERVICE_UNAVAILABLE);
		}
	}

	private boolean isReadMethod(String httpMethod) {
		return httpMethod.equalsIgnoreCase("GET") || httpMethod.equalsIgnoreCase("DELETE");
	}

	private RestClient.RequestBodySpec createRequestSpec(RestClient restClient, String httpMethod, String url) {
		String methodUpper = httpMethod.toUpperCase();
		switch (methodUpper) {
			case "POST":
				return restClient.post().uri(url);
			case "PUT":
				return restClient.put().uri(url);
			case "GET":
				return (RestClient.RequestBodySpec) restClient.get().uri(url);
			case "DELETE":
				return (RestClient.RequestBodySpec) restClient.delete().uri(url);
			default:
				throw new IllegalArgumentException("Unsupported HTTP method: " + httpMethod);
		}
	}

}
