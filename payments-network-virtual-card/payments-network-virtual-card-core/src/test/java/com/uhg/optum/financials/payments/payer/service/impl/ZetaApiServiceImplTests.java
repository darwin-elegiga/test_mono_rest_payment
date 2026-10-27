package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.config.ZetaApiProperties;
import com.uhg.optum.financials.payments.payer.dto.CancelCardRequest;
import com.uhg.optum.financials.payments.payer.dto.CreateCardRequest;
import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import com.uhg.optum.financials.payments.payer.dto.ResponseObject;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.BearerTokenGenerator;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentMatchers;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.HttpStatusCode;
import org.springframework.http.ResponseEntity;
import org.springframework.test.util.ReflectionTestUtils;
import org.springframework.web.client.RestClientResponseException;
import org.springframework.web.reactive.function.client.WebClient;
import org.mockito.junit.jupiter.MockitoSettings;
import org.mockito.quality.Strictness;
import tools.jackson.databind.ObjectMapper;

import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
@MockitoSettings(strictness = Strictness.LENIENT)
public class ZetaApiServiceImplTests {

	@InjectMocks
	ZetaApiServiceImpl zetaApiServiceImpl;

	@Mock
	LogUtility logUtility;

	@Mock
	SecretService secretService;

	@Mock
	WebClient webClient;

	@Mock
	AuditLogger auditLogger;

	@Mock
	RetryableApiService<Object> apiService;

	@Mock
	ResilientRestClientFactory resilientRestClientFactory;

	@Mock
	ResilientRestClient resilientRestClient;

	@Mock
	BearerTokenGenerator bearerTokenGenerator;

	@Mock
	ZetaApiProperties zetaApiProperties;

	@BeforeEach
	void setUp() {
		ReflectionTestUtils.setField(this.zetaApiServiceImpl, "bearerTokenGenerator", this.bearerTokenGenerator);
		ReflectionTestUtils.setField(this.zetaApiServiceImpl, "zetaApiProperties", this.zetaApiProperties);
		ReflectionTestUtils.setField(this.zetaApiServiceImpl, "retryableApiService", this.apiService);
		BDDMockito.given(this.resilientRestClientFactory.getClient("zeta-card-lookup"))
			.willReturn(this.resilientRestClient);
	}

	@Test
	public void testGetCardDetailsSuccess() throws Exception {
		BDDMockito.given(this.zetaApiProperties.getGetCardUrl())
			.willReturn(
					"https://gwacropolis-s2.mum1-s2.zetaapps.in/supercard-nn/tenants/${zeta.api.tenant}/cards/{cardGuid}");
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn("mockToken");
		BDDMockito.given(this.resilientRestClient.execute(any())).willReturn(ResponseEntity.ok("{\"cardID\":\"123\"}"));
		ResponseEntity<String> response = this.zetaApiServiceImpl.getVirtualCard(UUID.randomUUID());
		assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		assertThat(response.getBody()).contains("cardID");
	}

	@Test
	public void testGetCardCreationStatusSuccess() throws Exception {
		UUID requestId = UUID.randomUUID();
		String baseUrl = "https://mock-url.com/create";
		String expectedUrl = baseUrl + "?requestId=" + requestId;
		String mockToken = "mockToken";

		BDDMockito.given(this.zetaApiProperties.getCreateCardUrl()).willReturn(baseUrl);
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn(mockToken);

		ResponseEntity<String> responseEntity = Mockito.mock(ResponseEntity.class);
		BDDMockito.given(responseEntity.getStatusCode()).willReturn(HttpStatus.OK);
		BDDMockito.given(responseEntity.getBody()).willReturn("{\"status\":\"CREATED\"}");

		BDDMockito.given(this.apiService.callZetaApiWithRetry(Mockito.eq("zeta-card-balance"), Mockito.isNull(),
				Mockito.eq(mockToken), Mockito.eq(expectedUrl), Mockito.eq(HttpMethod.GET.toString()),
				Mockito.eq(String.class)))
			.willReturn(responseEntity);

		ResponseEntity<String> result = this.zetaApiServiceImpl.getCardCreationStatus(requestId);

		Mockito.verify(this.apiService, Mockito.times(1))
			.callZetaApiWithRetry(Mockito.eq("zeta-card-balance"), Mockito.isNull(), Mockito.eq(mockToken),
					Mockito.eq(expectedUrl), Mockito.eq(HttpMethod.GET.toString()), Mockito.eq(String.class));
		assertThat(result.getStatusCode()).isEqualTo(HttpStatus.OK);
		assertThat(result.getBody()).isEqualTo("{\"status\":\"CREATED\"}");

	}

	@Test
	public void testGetCardCreationStatusFailure() {
		UUID requestId = UUID.randomUUID();
		String mockToken = "mockToken";
		String baseUrl = "https://mock-url.com/create";
		String expectedUrl = baseUrl + "?requestId=" + requestId;

		ResponseEntity<String> responseEntity = Mockito.mock(ResponseEntity.class);
		BDDMockito.given(responseEntity.getStatusCode()).willReturn(HttpStatus.BAD_REQUEST);
		assertThat(responseEntity.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
	}

	@Test
	public void testGetVirtualCardError() throws Exception {
		UUID cardId = UUID.randomUUID();
		BDDMockito.given(this.zetaApiProperties.getGetCardUrl()).willReturn("https://mock-url/{cardGuid}");
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn("token");
		BDDMockito.given(this.resilientRestClient.execute(any()))
			.willReturn(ResponseEntity.status(HttpStatus.BAD_REQUEST).body("bad request"));
		Assertions.assertThat(this.zetaApiServiceImpl.getVirtualCard(cardId).getStatusCode())
			.isEqualTo(HttpStatus.BAD_REQUEST);
	}

	@Test
	public void testPostVirtualCardSuccess() throws Exception {
		CreateCardRequest req = Mockito.mock(CreateCardRequest.class);
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn("token");
		BDDMockito.given(this.zetaApiProperties.getCreateCardUrl()).willReturn("url");
		ResponseObject respObj = Mockito.mock(ResponseObject.class);
		BDDMockito.given(respObj.getHttpStatusCode()).willReturn(HttpStatusCode.valueOf(200));
		BDDMockito.given(this.apiService.callZetaApiWithRetry(req, "token", "url")).willReturn(respObj);
		ResponseEntity<?> response = this.zetaApiServiceImpl.postVirtualCard(req);
		Assertions.assertThat(response.getStatusCode().value()).isEqualTo(200);
	}

	@Test
	public void testCancelVirtualCardSuccess() throws Exception {
		CancelCardRequest req = Mockito.mock(CancelCardRequest.class);
		BDDMockito.given(req.getCardGuid()).willReturn(UUID.randomUUID());
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn("token");
		BDDMockito.given(this.zetaApiProperties.getCancelCardUrl()).willReturn("url/{cardGuid}");
		ObjectMapper mapper = new ObjectMapper();
		ReflectionTestUtils.setField(this.zetaApiServiceImpl, "objectMapper", mapper);
		ResponseObject respObj = Mockito.mock(ResponseObject.class);
		BDDMockito.given(respObj.getHttpStatusCode()).willReturn(HttpStatusCode.valueOf(200));
		BDDMockito
			.given(this.apiService.callZetaApiWithRetry(ArgumentMatchers.any(), ArgumentMatchers.eq("token"),
					ArgumentMatchers.anyString()))
			.willReturn(respObj);
		ResponseEntity<?> response = this.zetaApiServiceImpl.cancelVirtualCard(req);
		Assertions.assertThat(response.getStatusCode().value()).isEqualTo(200);
	}

	@Test
	public void testResetVirtualCardSuccess() throws Exception {
		ResetCardRequest req = Mockito.mock(ResetCardRequest.class);
		BDDMockito.given(req.getCardGuid()).willReturn(UUID.randomUUID());
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn("token");
		BDDMockito.given(this.zetaApiProperties.getResetCardUrl()).willReturn("url/{cardGuid}");
		ObjectMapper mapper = new ObjectMapper();
		ReflectionTestUtils.setField(this.zetaApiServiceImpl, "objectMapper", mapper);
		ResponseObject respObj = Mockito.mock(ResponseObject.class);
		BDDMockito.given(respObj.getHttpStatusCode()).willReturn(HttpStatusCode.valueOf(200));
		BDDMockito
			.given(this.apiService.callZetaApiWithRetry(ArgumentMatchers.any(), ArgumentMatchers.eq("token"),
					ArgumentMatchers.anyString()))
			.willReturn(respObj);
		ResponseEntity<?> response = this.zetaApiServiceImpl.resetVirtualCard(req);
		Assertions.assertThat(response.getStatusCode().value()).isEqualTo(200);
	}

	@Test
	public void testGetCardBalanceSuccess() throws Exception {
		UUID cguid = UUID.randomUUID();
		String baseUrl = "https://mock-url.com/balance/{cguid}";
		String expectedUrl = baseUrl.replace("{cguid}", cguid.toString());
		String mockToken = "mockToken";

		BDDMockito.given(this.zetaApiProperties.getGetCardBalanceUrl()).willReturn(baseUrl);
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn(mockToken);

		ResponseEntity<String> responseEntity = Mockito.mock(ResponseEntity.class);
		BDDMockito.given(responseEntity.getStatusCode()).willReturn(HttpStatus.OK);
		BDDMockito.given(responseEntity.getBody()).willReturn("{\"balance\":\"100.00\"}");
		BDDMockito
			.given(this.apiService.callZetaApiWithRetry(Mockito.isNull(), Mockito.eq(mockToken),
					Mockito.eq(expectedUrl), Mockito.eq(HttpMethod.GET.toString()), Mockito.eq(String.class)))
			.willReturn(responseEntity);

		ResponseEntity<String> result = this.zetaApiServiceImpl.getCardBalance(cguid);
		Mockito.verify(this.apiService, Mockito.times(1))
			.callZetaApiWithRetry(Mockito.isNull(), Mockito.eq(mockToken), Mockito.eq(expectedUrl),
					Mockito.eq(HttpMethod.GET.toString()), Mockito.eq(String.class));
		assertThat(result.getStatusCode()).isEqualTo(HttpStatus.OK);
		assertThat(result.getBody()).isEqualTo("{\"balance\":\"100.00\"}");
	}

	@Test
	public void testGetVirtualCardWithRestClientResponseExceptionWithBody() throws Exception {
		UUID cardId = UUID.randomUUID();
		BDDMockito.given(this.zetaApiProperties.getGetCardUrl()).willReturn("https://mock-url/{cardGuid}");
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn("token");

		RestClientResponseException exception = Mockito.mock(RestClientResponseException.class);
		BDDMockito.given(exception.getStatusCode()).willReturn(org.springframework.http.HttpStatus.BAD_REQUEST);
		BDDMockito.given(exception.getResponseBodyAsString()).willReturn("{\"error\":\"Bad Request\"}");
		BDDMockito.given(this.resilientRestClient.execute(any())).willThrow(exception);
		Assertions.assertThat(this.zetaApiServiceImpl.getVirtualCard(cardId).getStatusCode())
			.isEqualTo(HttpStatus.BAD_REQUEST);
	}

	@Test
	public void testGetVirtualCardWithRestClientResponseExceptionWithoutBody() throws Exception {
		UUID cardId = UUID.randomUUID();
		BDDMockito.given(this.zetaApiProperties.getGetCardUrl()).willReturn("https://mock-url/{cardGuid}");
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn("token");

		RestClientResponseException exception = Mockito.mock(RestClientResponseException.class);
		BDDMockito.given(exception.getStatusCode())
			.willReturn(org.springframework.http.HttpStatus.INTERNAL_SERVER_ERROR);
		BDDMockito.given(exception.getResponseBodyAsString()).willReturn(null);
		BDDMockito.given(this.resilientRestClient.execute(any())).willThrow(exception);

		ResponseEntity<String> result = this.zetaApiServiceImpl.getVirtualCard(cardId);

		assertThat(result.getStatusCode().value()).isEqualTo(500);
		assertThat(result.getBody()).isNull();
	}

	@Test
	public void testGetCardDetailsWithNullResponseBody() throws Exception {
		UUID cardId = UUID.randomUUID();
		BDDMockito.given(this.zetaApiProperties.getGetCardUrl()).willReturn("https://mock-url/{cardGuid}");
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn("token");

		ResponseEntity<String> responseEntity = Mockito.mock(ResponseEntity.class);
		BDDMockito.given(responseEntity.getBody()).willReturn(null);

		BDDMockito.given(this.resilientRestClient.execute(any())).willReturn(responseEntity);

		assertThatThrownBy(() -> this.zetaApiServiceImpl.getCardDetails(cardId)).isInstanceOf(RuntimeException.class)
			.hasMessage("Response body is null or empty");
	}

	@Test
	public void testGetCardDetailsWithEmptyResponseBody() throws Exception {
		UUID cardId = UUID.randomUUID();
		BDDMockito.given(this.zetaApiProperties.getGetCardUrl()).willReturn("https://mock-url/{cardGuid}");
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn("token");

		ResponseEntity<String> responseEntity = Mockito.mock(ResponseEntity.class);
		BDDMockito.given(responseEntity.getBody()).willReturn("");

		BDDMockito.given(this.resilientRestClient.execute(any())).willReturn(responseEntity);

		assertThatThrownBy(() -> this.zetaApiServiceImpl.getCardDetails(cardId)).isInstanceOf(RuntimeException.class)
			.hasMessage("Response body is null or empty");
	}

	@Test
	public void testGetAllRulesWithoutRuleType() throws Exception {
		Integer page = 0;
		Integer size = 10;
		String baseUrl = "https://mock-url.com/rules";
		String expectedUrl = baseUrl + "?page=" + page + "&size=" + size;
		String mockToken = "mockToken";

		BDDMockito.given(this.zetaApiProperties.getGetAllRulesUrl()).willReturn(baseUrl);
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn(mockToken);

		ResponseEntity<String> responseEntity = Mockito.mock(ResponseEntity.class);
		BDDMockito.given(responseEntity.getStatusCode()).willReturn(HttpStatus.OK);
		BDDMockito.given(responseEntity.getBody()).willReturn("{\"rules\":[]}");

		BDDMockito
			.given(this.apiService.callZetaApiWithRetry(Mockito.isNull(), Mockito.eq(mockToken),
					Mockito.eq(expectedUrl), Mockito.eq(HttpMethod.GET.toString()), Mockito.eq(String.class)))
			.willReturn(responseEntity);

		ResponseEntity<String> result = this.zetaApiServiceImpl.getAllRules(page, size, null);

		Mockito.verify(this.apiService, Mockito.times(1))
			.callZetaApiWithRetry(Mockito.isNull(), Mockito.eq(mockToken), Mockito.eq(expectedUrl),
					Mockito.eq(HttpMethod.GET.toString()), Mockito.eq(String.class));
		assertThat(result.getStatusCode()).isEqualTo(HttpStatus.OK);
		assertThat(result.getBody()).isEqualTo("{\"rules\":[]}");
	}

	@Test
	public void testGetAllRulesWithEmptyRuleType() throws Exception {
		Integer page = 0;
		Integer size = 10;
		String baseUrl = "https://mock-url.com/rules";
		String expectedUrl = baseUrl + "?page=" + page + "&size=" + size;
		String mockToken = "mockToken";

		BDDMockito.given(this.zetaApiProperties.getGetAllRulesUrl()).willReturn(baseUrl);
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn(mockToken);

		ResponseEntity<String> responseEntity = Mockito.mock(ResponseEntity.class);
		BDDMockito.given(responseEntity.getStatusCode()).willReturn(HttpStatus.OK);
		BDDMockito.given(responseEntity.getBody()).willReturn("{\"rules\":[]}");

		BDDMockito
			.given(this.apiService.callZetaApiWithRetry(Mockito.isNull(), Mockito.eq(mockToken),
					Mockito.eq(expectedUrl), Mockito.eq(HttpMethod.GET.toString()), Mockito.eq(String.class)))
			.willReturn(responseEntity);

		ResponseEntity<String> result = this.zetaApiServiceImpl.getAllRules(page, size, "");

		Mockito.verify(this.apiService, Mockito.times(1))
			.callZetaApiWithRetry(Mockito.isNull(), Mockito.eq(mockToken), Mockito.eq(expectedUrl),
					Mockito.eq(HttpMethod.GET.toString()), Mockito.eq(String.class));
		assertThat(result.getStatusCode()).isEqualTo(HttpStatus.OK);
		assertThat(result.getBody()).isEqualTo("{\"rules\":[]}");
	}

	@Test
	public void testGetAllRulesWithRuleType() throws Exception {
		Integer page = 1;
		Integer size = 20;
		String ruleType = "SPENDING";
		String baseUrl = "https://mock-url.com/rules";
		String expectedUrl = baseUrl + "?ruleType=" + ruleType + "&page=" + page + "&size=" + size;
		String mockToken = "mockToken";

		BDDMockito.given(this.zetaApiProperties.getGetAllRulesUrl()).willReturn(baseUrl);
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn(mockToken);

		ResponseEntity<String> responseEntity = Mockito.mock(ResponseEntity.class);
		BDDMockito.given(responseEntity.getStatusCode()).willReturn(HttpStatus.OK);
		BDDMockito.given(responseEntity.getBody()).willReturn("{\"rules\":[{\"ruleType\":\"SPENDING\"}]}");

		BDDMockito
			.given(this.apiService.callZetaApiWithRetry(Mockito.isNull(), Mockito.eq(mockToken),
					Mockito.eq(expectedUrl), Mockito.eq(HttpMethod.GET.toString()), Mockito.eq(String.class)))
			.willReturn(responseEntity);

		ResponseEntity<String> result = this.zetaApiServiceImpl.getAllRules(page, size, ruleType);

		Mockito.verify(this.apiService, Mockito.times(1))
			.callZetaApiWithRetry(Mockito.isNull(), Mockito.eq(mockToken), Mockito.eq(expectedUrl),
					Mockito.eq(HttpMethod.GET.toString()), Mockito.eq(String.class));
		assertThat(result.getStatusCode()).isEqualTo(HttpStatus.OK);
		assertThat(result.getBody()).contains("SPENDING");
	}

	@Test
	public void testGetRuleByIdSuccess() throws Exception {
		String ruleId = "rule123";
		String baseUrl = "https://mock-url.com/rules/{ruleId}";
		String expectedUrl = baseUrl.replace("{ruleId}", ruleId);
		String mockToken = "mockToken";

		BDDMockito.given(this.zetaApiProperties.getGetRuleByRuleIdUrl()).willReturn(baseUrl);
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn(mockToken);

		ResponseEntity<String> responseEntity = Mockito.mock(ResponseEntity.class);
		BDDMockito.given(responseEntity.getStatusCode()).willReturn(HttpStatus.OK);
		BDDMockito.given(responseEntity.getBody()).willReturn("{\"ruleId\":\"rule123\"}");

		BDDMockito
			.given(this.apiService.callZetaApiWithRetry(Mockito.isNull(), Mockito.eq(mockToken),
					Mockito.eq(expectedUrl), Mockito.eq(HttpMethod.GET.toString()), Mockito.eq(String.class)))
			.willReturn(responseEntity);

		ResponseEntity<String> result = this.zetaApiServiceImpl.getRuleById(ruleId);

		Mockito.verify(this.apiService, Mockito.times(1))
			.callZetaApiWithRetry(Mockito.isNull(), Mockito.eq(mockToken), Mockito.eq(expectedUrl),
					Mockito.eq(HttpMethod.GET.toString()), Mockito.eq(String.class));
		assertThat(result.getStatusCode()).isEqualTo(HttpStatus.OK);
		assertThat(result.getBody()).contains("rule123");
	}

	@Test
	public void testGetVirtualCardSuccess() throws Exception {
		UUID cardId = UUID.randomUUID();
		BDDMockito.given(this.zetaApiProperties.getGetCardUrl()).willReturn("https://mock-url/{cardGuid}");
		BDDMockito.given(this.bearerTokenGenerator.generateToken()).willReturn("token");

		ResponseEntity<String> successResponse = ResponseEntity.ok("{\"cardID\":\"123\"}");
		BDDMockito.given(this.resilientRestClient.execute(any())).willReturn(successResponse);

		ResponseEntity<String> result = this.zetaApiServiceImpl.getVirtualCard(cardId);
		Assertions.assertThat(result.getBody()).isEqualTo("{\"cardID\":\"123\"}");
	}

}
