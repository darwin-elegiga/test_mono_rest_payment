package com.uhg.optum.financials.payments.APITrigger;

import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import okhttp3.MediaType;
import okhttp3.RequestBody;
import okhttp3.ResponseBody;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ContextConfiguration;
import retrofit2.Call;
import retrofit2.Response;

import java.io.IOException;
import java.util.UUID;

@SpringBootTest
@ContextConfiguration(classes = APITriggers.class)
public final class APITriggers {

	private static APITriggers instance;

	private static final Logger log = LoggerFactory.getLogger(APITriggers.class);

	private APITriggers() {
	}

	APIEndPoints apiEndPoints;

	public ObjectMapper objectMapper = getObjectMapper();

	public ObjectMapper getObjectMapper() {
		this.objectMapper = new ObjectMapper().registerModule(new JavaTimeModule());
		this.objectMapper.configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
		this.objectMapper.configure(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS, false);
		return this.objectMapper;
	}

	public static APITriggers getAPITriggersInstance() {
		synchronized (APITriggers.class) {
			if (instance == null) {
				instance = new APITriggers();
			}
			else {
				return instance;
			}
		}
		return instance;
	}

	public Response<ResponseBody> loadPaymentInVirtualCard(Object reqBody) throws IOException {

		String body = this.objectMapper.writeValueAsString(reqBody);
		log.info(body);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);

		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.loadPayment(jsonBody);
		return call.execute();

	}

	public Response<ResponseBody> getPaymentDetails(String paymentID) throws IOException {
		ObjectMapper objectMapper = new ObjectMapper();
		objectMapper.registerModule(new JavaTimeModule());
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getPaymentDetails(paymentID);
		return call.execute();
	}

	public Response<ResponseBody> getPaymentFiles(String correlationId) throws IOException {
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getPaymentFiles(correlationId);
		return call.execute();
	}

	public Response<ResponseBody> loadAccount(Object reqBody) throws IOException {
		String body = this.objectMapper.writeValueAsString(reqBody);
		log.info(body);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);

		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.loadAccount(jsonBody);
		return call.execute();

	}

	public Response<ResponseBody> loadOrganizationNonProd(Object reqBody, String X_Optum_Tenant_Id) throws IOException {
		String body = reqBody.toString();
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.loadOrganizationNonProd(X_Optum_Tenant_Id, jsonBody);
		return call.execute();
	}

	public Response<ResponseBody> loadAccountNonProd(Object reqBody, String X_Optum_Tenant_Id) throws IOException {
		String body = this.objectMapper.writeValueAsString(reqBody);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.loadAccountNonProd(X_Optum_Tenant_Id, jsonBody);
		return call.execute();
	}

	public Response<ResponseBody> getAccountDetails(String orgShortCode, String accountShortCode) throws IOException {
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getAccountDetails(orgShortCode, accountShortCode);

		return call.execute();
	}

	public Response<ResponseBody> loadSubAccountNonProd(Object reqBody, String X_Optum_Tenant_Id,
			String accountShortCode) throws IOException {
		String body = this.objectMapper.writeValueAsString(reqBody);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.loadSubAccountNonProd(X_Optum_Tenant_Id, accountShortCode,
				jsonBody);
		return call.execute();
	}

	public Response<ResponseBody> loadSubAccount(Object reqBody) throws IOException {
		String body = this.objectMapper.writeValueAsString(reqBody);
		log.info(body);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);

		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.loadSubAccount(jsonBody);
		return call.execute();

	}

	public Response<ResponseBody> loadOrganization(Object reqBody) throws IOException {
		String body = this.objectMapper.writeValueAsString(reqBody);
		log.info(body);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);

		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.loadOrganization(jsonBody);
		return call.execute();

	}

	public Response<ResponseBody> getOrganizationDetails(String orgShortCode) throws IOException {
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getOrganizationDetails(orgShortCode);
		return call.execute();
	}

	public Response<ResponseBody> getOrgDetails(String orgShortCode) throws IOException {
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getOrganizationDetailsInVC(orgShortCode);
		return call.execute();
	}

	public Response<ResponseBody> loadSubAccountUpdated(Object reqBody) throws IOException {
		String body = this.objectMapper.writeValueAsString(reqBody);
		log.info(body);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);

		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.loadSubAccountUpdated(jsonBody);
		return call.execute();

	}

	public Response<ResponseBody> loadBankAccount(Object reqBody) throws IOException {
		String body = this.objectMapper.writeValueAsString(reqBody);
		log.info(body);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);

		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.loadBankAccount(jsonBody);
		return call.execute();

	}

	public Response<ResponseBody> loadLogoUpdatedEvent(Object reqBody) throws IOException {
		String body = this.objectMapper.writeValueAsString(reqBody);
		log.info(body);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);

		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.loadLogoUpdatedEvent(jsonBody);
		return call.execute();
	}

	public Response<ResponseBody> getLogoDetails(Object reqBody) throws IOException {
		String body = this.objectMapper.writeValueAsString(reqBody);
		log.info(body);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);

		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.getLogoDetails(jsonBody);
		return call.execute();
	}

	public Response<ResponseBody> loadSubAccountUpdatedNonProd(Object reqBody, String X_Optum_Tenant_Id,
			String accountShortCode, String subAccountShortCode) throws IOException {
		String body = this.objectMapper.writeValueAsString(reqBody);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.loadSubAccountUpdatedNonProd(X_Optum_Tenant_Id, accountShortCode,
				subAccountShortCode, jsonBody);
		return call.execute();
	}

	public Response<ResponseBody> getCardDetails(String requestId) throws IOException {
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getCardCreationDetails(requestId);
		return call.execute();
	}

	public Response<ResponseBody> getCardPollingDetails(String paymentId) throws IOException {
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getCardPollingDetails(paymentId);
		return call.execute();
	}

	public Response<ResponseBody> spendRulesConfigure(Object reqBody, String tenantId) throws IOException {
		String body = this.objectMapper.writeValueAsString(reqBody);
		log.info(body);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.configureSpendRules(jsonBody, tenantId);
		return call.execute();
	}

	public Response<ResponseBody> createCallBack(Object reqBody) throws IOException {
		String body = this.objectMapper.writeValueAsString(reqBody);
		log.info(body);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.createCallBack(jsonBody);
		return call.execute();
	}

	public Response<ResponseBody> getVirtualCardDetails(String paymentId) throws IOException {
		ObjectMapper objectMapper = new ObjectMapper();
		objectMapper.registerModule(new JavaTimeModule());
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getVirtualCardDetails(paymentId);
		return call.execute();
	}

	public Response<ResponseBody> cancelCardRequest(Object reqBody) throws IOException {

		String body = this.objectMapper.writeValueAsString(reqBody);
		log.info("Cancel Req body received: {}", body);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.cancelCardRequest(jsonBody);
		return call.execute();

	}

	public Response<ResponseBody> resetCardRequest(Object reqBody) throws IOException {

		String body = this.objectMapper.writeValueAsString(reqBody);
		log.info("Reset Req body received: {}", body);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.resetCardRequest(jsonBody);
		return call.execute();

	}

	public Response<ResponseBody> generateAuthEvents(String startDate, int numberOfAuthEvents, String cardId)
			throws IOException {
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.generateAuthEvents(startDate, numberOfAuthEvents, cardId);
		return call.execute();
	}

	public Response<ResponseBody> createIssuingWebhook(Object reqBody) throws IOException {

		String body = this.objectMapper.writeValueAsString(reqBody);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.createIssuingWebhook(jsonBody);
		return call.execute();

	}

	public Response<ResponseBody> getCancelCardDetails(String paymentId) throws IOException {
		ObjectMapper objectMapper = new ObjectMapper();
		objectMapper.registerModule(new JavaTimeModule());
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getCancelCardDetails(paymentId);
		return call.execute();
	}

	public Response<ResponseBody> getCardResetDetails(String receiptId) throws IOException {
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getCardResetDetails(receiptId);
		return call.execute();
	}

	public Response<ResponseBody> getAuthExtractRecord(String receiptId) throws IOException {
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getAuthExtractRecord(receiptId);
		return call.execute();
	}

	public Response<ResponseBody> generateClearExtractByDateRange(String startDate) throws IOException {
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.generateClearExtractByDateRange(startDate);
		return call.execute();
	}

	public Response<ResponseBody> publishBatchEvent(Object reqBody) throws IOException {
		String body = this.objectMapper.writeValueAsString(reqBody);
		log.info(body);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.publishBatchEvent(jsonBody);
		return call.execute();
	}

	public Response<ResponseBody> getBatchEvent(String correlationId) throws IOException {
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getBatchEvent(correlationId);
		return call.execute();
	}

	public Response<ResponseBody> loadBankAccountNonProd(Object reqBody, String X_Optum_Tenant_Id) throws IOException {
		String body = this.objectMapper.writeValueAsString(reqBody);
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		RequestBody jsonBody = RequestBody.create(MediaType.parse("application/json"), body);
		Call<ResponseBody> call = this.apiEndPoints.loadBankAccountNonProd(X_Optum_Tenant_Id, jsonBody);
		return call.execute();
	}

	public Response<ResponseBody> getBankAccountDetails(UUID bankAccountId) throws IOException {
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getBankAccountDetails(bankAccountId);
		return call.execute();
	}

	public Response<ResponseBody> getSubAccountDetails(String orgShortCode, String AccountShortCode,
			String subAccountShortCode) throws IOException {

		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getSubAccountDetails(orgShortCode, AccountShortCode,
				subAccountShortCode);
		return call.execute();
	}

	public Response<ResponseBody> getBankAccountDetailsFromOrg(UUID bankAccountId, String orgShortCode)
			throws IOException {
		this.apiEndPoints = RetrofitClient.getClient().create(APIEndPoints.class);
		Call<ResponseBody> call = this.apiEndPoints.getBankAccountDetailsFromOrg(bankAccountId, orgShortCode);
		return call.execute();
	}

}
