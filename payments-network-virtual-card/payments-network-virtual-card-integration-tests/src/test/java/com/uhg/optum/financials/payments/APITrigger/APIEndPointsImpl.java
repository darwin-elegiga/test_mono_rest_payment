package com.uhg.optum.financials.payments.APITrigger;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ObjectNode;
import com.uhg.optum.financials.payments.organization.events.AccountRegistered;
import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import com.uhg.optum.financials.payments.organization.events.SubAccountUpdated;
import com.uhg.optum.financials.payments.payer.dto.AccountRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.CancelCardDto;
import com.uhg.optum.financials.payments.payer.dto.CardCreationStatusDto;
import com.uhg.optum.financials.payments.payer.dto.PaymentResponseDto;
import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import com.uhg.optum.financials.payments.payer.dto.SpendRulesDTO;
import com.uhg.optum.financials.payments.payer.dto.SubAccountRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.SubAccountUpdatedDto;
import com.uhg.optum.financials.payments.payer.dto.FileTrackingDto;
import com.uhg.optum.financials.payments.payer.dto.account.AccountDto;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.BankAccountEventDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchEventDto;
import com.uhg.optum.financials.payments.payer.dto.callback.CancelCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationDTO;
import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationRegisteredEvent;
import com.uhg.optum.financials.payments.payer.model.events.Organization;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.WebhookBaseDto;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.utilites.DTOUtility;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import integration.dto.BankAccountNonProd;
import okhttp3.ResponseBody;
import org.assertj.core.api.Assertions;
import org.awaitility.Awaitility;
import org.awaitility.core.ConditionTimeoutException;
import org.json.JSONException;
import org.json.JSONObject;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import retrofit2.Response;

import java.io.IOException;
import java.math.BigDecimal;
import java.math.RoundingMode;
import java.time.Duration;
import java.time.Instant;
import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeFormatterBuilder;
import java.util.concurrent.TimeUnit;
import java.util.UUID;

import static com.uhg.optum.financials.payments.APITrigger.RetrofitClient.getEnv;
import static com.uhg.optum.financials.payments.APITrigger.RetrofitClient.shouldMockZetaCallbacks;
import static com.uhg.optum.financials.payments.payer.constant.EventTypeConstants.VIRTUAL_CARD_CANCEL;
import static com.uhg.optum.financials.payments.payer.constant.EventTypeConstants.VIRTUAL_CARD_RESET;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.AUTH_RESET;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_ACCEPTED;
import static org.awaitility.Awaitility.await;

public final class APIEndPointsImpl extends DTOUtility {

	private static final Logger log = LoggerFactory.getLogger(APIEndPointsImpl.class);

	private static final DateTimeFormatter WEBHOOK_TIME_FORMATTER = new DateTimeFormatterBuilder().appendInstant(3)
		.toFormatter();

	APITriggers apiTriggers = APITriggers.getAPITriggersInstance();

	final int waitToLoadEntity = 2;

	final int waitToCompleteJob = 8;

	private Duration pollTime = Duration.ofSeconds(5);

	private static APIEndPointsImpl apiCalls;

	Duration poolTime = Duration.ofMillis(250);

	ObjectMapper objectMapper = this.apiTriggers.objectMapper;

	private APIEndPointsImpl() {
	}

	public static APIEndPointsImpl getAPIEndPointsImplInstance() {
		synchronized (APITriggers.class) {
			if (apiCalls == null) {
				apiCalls = new APIEndPointsImpl();
			}
			else {
				return apiCalls;
			}
		}
		return apiCalls;
	}

	public OrganizationRegisteredEvent loadOrganizationInVirtualCard() throws IOException {
		OrganizationRegisteredEvent organizationEventDto = loadOrganization();
		Response<ResponseBody> response = this.apiTriggers.loadOrganization(organizationEventDto);
		logFailureResponse("loadOrganizationInVirtualCard", response);
		Assertions.assertThat(response.code()).isEqualTo(200);

		UUID orgId = organizationEventDto.getId();
		isOrganizationRegistered(orgId);

		return organizationEventDto;
	}

	public String loadOrganizationFromOrg() throws IOException, JSONException {
		OrganizationDTO organization = getOrgRegistered();
		String reqBody = this.objectMapper.writeValueAsString(organization);

		JSONObject jsonObject = new JSONObject(reqBody);
		jsonObject.put("onboardedOn", "2024-11-17T12:04:19.9171884+00:00");

		log.info("loadOrganizationFromOrg request payload: {}", jsonObject);

		Response<ResponseBody> response = this.apiTriggers.loadOrganizationNonProd(jsonObject,
				organization.getShortCode());
		if (response.code() != 200) {
			String responseBody = response.errorBody() != null ? response.errorBody().string() : "<empty>";
			log.error("loadOrganizationFromOrg failed with status {} and body {}", response.code(), responseBody);
		}
		Assertions.assertThat(response.code()).isEqualTo(200);

		String orgShortCode = organization.getShortCode();
		isOrgRegistered(orgShortCode);

		return orgShortCode;
	}

	public AccountRegisteredEvent loadAccountInVirtualCard(UUID orgId, String orgShortCode) throws IOException {
		AccountRegisteredEvent payerEventDto = loadPayer(orgId);
		Response<ResponseBody> response = this.apiTriggers.loadAccount(payerEventDto);
		logFailureResponse("loadAccountInVirtualCard", response);
		Assertions.assertThat(response.code()).isEqualTo(200);

		String accountShortCode = payerEventDto.getAccountRegistered().getShortCode();
		isAccountRegistered(accountShortCode, orgShortCode);
		return payerEventDto;
	}

	public AccountRegistered loadAccountFromOrg(String orgShortcode) throws IOException {
		AccountRegistered accountRegistered = getAccountRegistered();
		Response<ResponseBody> response = this.apiTriggers.loadAccountNonProd(accountRegistered, orgShortcode);
		Assertions.assertThat(response.code()).isEqualTo(200);
		String accountShortCode = accountRegistered.getShortCode();
		isAccountRegistered(accountShortCode, orgShortcode);
		return accountRegistered;
	}

	public SubAccountRegisteredEvent loadSubAccountInVirtualCard(UUID orgId, String orgShortCode,
			String accountShortCode, UUID bankId) throws IOException {
		SubAccountRegisteredEvent subPayerEventDto = loadSubAccount(orgId, accountShortCode, bankId);
		Response<ResponseBody> response = this.apiTriggers.loadSubAccount(subPayerEventDto);
		logFailureResponse("loadSubAccountInVirtualCard", response);
		Assertions.assertThat(response.code()).isEqualTo(200);

		String subAccountShortCode = subPayerEventDto.getSubAccountRegistered().getShortCode();
		isSubAccountRegistered(orgShortCode, accountShortCode, subAccountShortCode);

		return subPayerEventDto;
	}

	public String loadSubAccountInVirtualCard(String orgShortCode, String accountShortCode,
			SubAccountDto subAccountEventDto, UUID bankId) throws IOException {

		Response<ResponseBody> response = this.apiTriggers.loadSubAccount(subAccountEventDto);
		logFailureResponse("loadSubAccountInVirtualCard", response);
		Assertions.assertThat(response.code()).isEqualTo(200);

		String subAccountShortCode = subAccountEventDto.getSubAccountRegistered().getShortCode();
		isSubAccountRegistered(orgShortCode, accountShortCode, subAccountShortCode);

		return subAccountShortCode;
	}

	public String loadSubAccountNonProd(String orgShortCode, UUID bankId, String accountShortCode) throws IOException {
		SubAccountRegistered subAccountRegistered = getSubAccountRegistered(bankId);
		Response<ResponseBody> response = this.apiTriggers.loadSubAccountNonProd(subAccountRegistered, orgShortCode,
				accountShortCode);
		Assertions.assertThat(response.code()).isEqualTo(200);
		String subAccountShortCode = subAccountRegistered.getShortCode();
		isSubAccountRegistered(orgShortCode, accountShortCode, subAccountShortCode);
		return subAccountShortCode;
	}

	public BankAccountEventDto loadBankAccountInVirtualCard(UUID orgId) throws IOException {
		BankAccountEventDto bank = bankAccountEventDto(orgId);
		Response<ResponseBody> response = this.apiTriggers.loadBankAccount(bank);
		logFailureResponse("loadBankAccountInVirtualCard", response);
		Assertions.assertThat(response.code()).isEqualTo(200);

		isBankAccountRegistered(bank.getId());
		return bank;
	}

	public void loadPayment(PaymentRequestDto paymentRequestDto) throws IOException {
		submitPayment(paymentRequestDto, true);
	}

	public void pushPayment(PaymentRequestDto paymentRequestDto) throws IOException {
		submitPayment(paymentRequestDto, false);
	}

	private void submitPayment(PaymentRequestDto paymentRequestDto, boolean waitForLoad) throws IOException {
		retrofit2.Response<ResponseBody> response = this.apiTriggers.loadPaymentInVirtualCard(paymentRequestDto);
		logFailureResponse(waitForLoad ? "loadPayment" : "pushPayment", response);
		Assertions.assertThat(response.code()).isEqualTo(200);
		if (waitForLoad) {
			isPaymentLoaded(paymentRequestDto.getPaymentDetails().getId());
		}
	}

	public PaymentResponseDto getPaymentDetails(String paymentID) throws IOException {

		Response<ResponseBody> paymentRequest = this.apiTriggers.getPaymentDetails(paymentID);
		Assertions.assertThat(paymentRequest.code()).isEqualTo(200);
		return this.objectMapper.readValue(paymentRequest.body().string(), PaymentResponseDto.class);
	}

	public FileTrackingDto getPaymentFiles(String correlationId) throws IOException {
		Response<ResponseBody> response = this.apiTriggers.getPaymentFiles(correlationId);
		try {
			logFailureResponse("getPaymentFiles", response);
			Assertions.assertThat(response.code()).isEqualTo(200);
			Assertions.assertThat(response.body()).isNotNull();
			try (ResponseBody body = response.body()) {
				return this.objectMapper.readValue(body.byteStream(), FileTrackingDto.class);
			}
		}
		finally {
			closeResponseBodies(response);
		}
	}

	public void isOrganizationRegistered(UUID orgId) throws IOException {
		waitForEntityRegistration(() -> {
			Response<ResponseBody> organizationDetails = this.apiTriggers.getOrganizationDetails(String.valueOf(orgId));
			return organizationDetails.code() == 200;
		}, this.poolTime, "Org : " + orgId + "  not Registered in " + this.waitToLoadEntity + " minutes ");
	}

	public void isOrgRegistered(String orgShortCode) throws IOException {
		waitForEntityRegistration(() -> {
			Response<ResponseBody> organizationDetails = this.apiTriggers.getOrgDetails(orgShortCode);
			return organizationDetails.code() == 200;
		}, this.poolTime, "Org : " + orgShortCode + "  not Registered in " + this.waitToLoadEntity + " minutes ");
	}

	public OrganizationRegisteredEvent getOrganizationDetails(UUID orgId) throws IOException {
		Response<ResponseBody> organizationDetails = this.apiTriggers.getOrganizationDetails(String.valueOf(orgId));
		Assertions.assertThat(organizationDetails.code()).isEqualTo(200);
		return this.objectMapper.readValue(organizationDetails.body().string(), OrganizationRegisteredEvent.class);
	}

	public UUID getOrganizationIdByShortCode(String orgShortCode) throws IOException {
		Response<ResponseBody> organizationDetails = this.apiTriggers.getOrgDetails(orgShortCode);
		logFailureResponse("getOrganizationIdByShortCode", organizationDetails);
		Assertions.assertThat(organizationDetails.code()).isEqualTo(200);
		Organization organization = this.objectMapper.readValue(organizationDetails.body().string(),
				Organization.class);
		Assertions.assertThat(organization.getOrgId()).isNotNull();
		return organization.getOrgId();
	}

	public SubAccountUpdatedDto loadSubAccountUpdatedInVirtualCard(UUID orgId, String orgShortCode,
			String accountShortCode, UUID bankId, String subAccountShortCode) throws IOException {
		SubAccountUpdatedDto subPayerEventDto = loadSubAccountUpdated(orgId, accountShortCode, bankId,
				subAccountShortCode);
		Response<ResponseBody> response = this.apiTriggers.loadSubAccountUpdated(subPayerEventDto);
		logFailureResponse("loadSubAccountUpdatedInVirtualCard", response);
		Assertions.assertThat(response.code()).isEqualTo(200);
		isSubAccountRegistered(orgShortCode, accountShortCode, subAccountShortCode);
		return subPayerEventDto;
	}

	public void loadLogoUpdatedEventInVirtualCard(UUID orgId, String orgShortCode, String accountShortCode,
			String subAccountShortCode) throws IOException {
		ObjectNode requestBody = this.objectMapper.createObjectNode();
		requestBody.put("id", orgId.toString());
		requestBody.put("tenantId", UUID.randomUUID().toString());
		requestBody.put("eventId", UUID.randomUUID().toString());

		ObjectNode logoUpdated = requestBody.putObject("logoUpdated");
		logoUpdated.put("accountShortCode", accountShortCode);
		logoUpdated.put("subAccountShortCode", subAccountShortCode);
		logoUpdated.put("updatedBy", "Bob Smith");
		logoUpdated.put("type", "logo_updated");

		ObjectNode actor = logoUpdated.putObject("actor");
		actor.put("requestedBy", "string");
		actor.put("userId", "string");

		Response<ResponseBody> response = this.apiTriggers.loadLogoUpdatedEvent(requestBody);
		logFailureResponse("loadLogoUpdatedEventInVirtualCard", response);
		Assertions.assertThat(response.code()).isEqualTo(200);
		isLogoDetailsUpdated(orgShortCode, accountShortCode, subAccountShortCode);
	}

	public JsonNode getLogoDetails(String orgShortCode, String accountShortCode, String subAccountShortCode)
			throws IOException {
		Response<ResponseBody> response = this.apiTriggers
			.getLogoDetails(buildLogoDetailsRequest(orgShortCode, accountShortCode, subAccountShortCode));
		logFailureResponse("getLogoDetails", response);
		Assertions.assertThat(response.code()).isEqualTo(200);
		JsonNode logoDetails = this.objectMapper.readTree(response.body().string());
		Assertions.assertThat(logoDetails.path("orgShortCode").asText()).isEqualTo(orgShortCode);
		Assertions.assertThat(logoDetails.path("accountShortCode").asText()).isEqualTo(accountShortCode);
		Assertions.assertThat(logoDetails.path("subAccountShortCode").asText()).isEqualTo(subAccountShortCode);
		return logoDetails;
	}

	public void isLogoDetailsUpdated(String orgShortCode, String accountShortCode, String subAccountShortCode) {
		waitForEntityRegistration(() -> {
			Response<ResponseBody> response = this.apiTriggers
				.getLogoDetails(buildLogoDetailsRequest(orgShortCode, accountShortCode, subAccountShortCode));
			return response.code() == 200;
		}, this.poolTime, "Logo details not updated for orgShortCode: " + orgShortCode + ", accountShortCode: "
				+ accountShortCode + ", subAccountShortCode: " + subAccountShortCode);
	}

	private ObjectNode buildLogoDetailsRequest(String orgShortCode, String accountShortCode,
			String subAccountShortCode) {
		ObjectNode requestBody = this.objectMapper.createObjectNode();
		requestBody.put("accountShortCode", accountShortCode);
		requestBody.put("subAccountShortCode", subAccountShortCode);
		requestBody.put("orgShortCode", orgShortCode);
		return requestBody;
	}

	private void logFailureResponse(String operation, Response<ResponseBody> response) throws IOException {
		if (response.code() >= 400) {
			String responseBody = response.errorBody() != null ? response.errorBody().string() : "<empty>";
			log.info("{} failed with status {} and body {}", operation, response.code(), responseBody);
		}
	}

	public String loadSubAccountUpdatedNonProd(String orgShortCode, UUID bankId, String accountShortCode,
			String subAccountShortCode) throws IOException {
		SubAccountUpdated subAccountUpdated = getSubAccountUpdated(bankId);
		Response<ResponseBody> response = this.apiTriggers.loadSubAccountUpdatedNonProd(subAccountUpdated, orgShortCode,
				accountShortCode, subAccountShortCode);
		Assertions.assertThat(response.code()).isEqualTo(200);
		String subAccountUpdatedShortCode = subAccountShortCode;
		isSubAccountRegistered(orgShortCode, accountShortCode, subAccountShortCode);
		return subAccountUpdatedShortCode;
	}

	public void isPaymentLoaded(String paymentId) {
		waitForEntityRegistration(() -> {
			Response<ResponseBody> paymentDetails = this.apiTriggers.getPaymentDetails(paymentId);
			return paymentDetails.code() == 200;
		}, this.poolTime, "payment: " + paymentId + " not loaded in " + this.waitToLoadEntity + " minutes ");
	}

	public CardCreation getCardCreationDetails(String requestId) throws IOException {
		retrofit2.Response<ResponseBody> response = this.apiTriggers.getCardDetails(requestId);
		Assertions.assertThat(response.code()).isEqualTo(200);
		Assertions.assertThat(response.body()).isNotNull();
		try (ResponseBody body = response.body()) {
			return this.objectMapper.readValue(body.byteStream(), CardCreation.class);
		}
	}

	public CardCreationStatusDto getCardPollingDetails(String paymentId) throws IOException {
		retrofit2.Response<ResponseBody> response = this.apiTriggers.getCardPollingDetails(paymentId);
		try {
			logFailureResponse("getCardPollingDetails", response);
			Assertions.assertThat(response.code()).isEqualTo(200);
			Assertions.assertThat(response.body()).isNotNull();
			try (ResponseBody body = response.body()) {
				return this.objectMapper.readValue(body.byteStream(), CardCreationStatusDto.class);
			}
		}
		finally {
			if (response.code() != 200 || response.body() == null) {
				closeResponseBodies(response);
			}
		}
	}

	public CardCreationStatusDto awaitCardPollingDetails(String paymentId) {
		final CardCreationStatusDto[] pollingDetailsRef = new CardCreationStatusDto[1];
		try {
			Awaitility.await()
				.atMost(Duration.ofMinutes(this.waitToCompleteJob))
				.pollDelay(shouldMockZetaCallbacks() ? Duration.ofSeconds(10) : Duration.ZERO)
				.pollInterval(shouldMockZetaCallbacks() ? Duration.ofSeconds(10) : this.poolTime)
				.until(() -> {
					CardCreationStatusDto cardPollingDetails;
					try {
						cardPollingDetails = getCardPollingDetailsIfAvailable(paymentId);
					}
					catch (IOException ex) {
						log.info("Card polling details request failed for paymentId {} and will be retried: {}",
								paymentId, ex.getMessage());
						return false;
					}
					if (cardPollingDetails == null || cardPollingDetails.getData() == null
							|| cardPollingDetails.getData().getCardGuid() == null
							|| cardPollingDetails.getData().getCardGuid().isBlank()) {
						return false;
					}
					pollingDetailsRef[0] = cardPollingDetails;
					log.info("Card polling details available for paymentId {}", paymentId);
					return true;
				});
			return pollingDetailsRef[0];
		}
		catch (ConditionTimeoutException ex) {
			throw new ConditionTimeoutException("Card polling details not available for paymentId " + paymentId
					+ " within " + this.waitToCompleteJob + " minutes", ex);
		}
	}

	private CardCreationStatusDto getCardPollingDetailsIfAvailable(String paymentId) throws IOException {
		retrofit2.Response<ResponseBody> response = this.apiTriggers.getCardPollingDetails(paymentId);
		try {
			if (response.code() != 200 || response.body() == null) {
				logFailureResponse("getCardPollingDetails", response);
				return null;
			}
			try (ResponseBody body = response.body()) {
				return this.objectMapper.readValue(body.byteStream(), CardCreationStatusDto.class);
			}
		}
		finally {
			closeResponseBodies(response);
		}
	}

	public void configureSpendRules(SpendRulesDTO spendRulesDTO, String tenantId) throws IOException {
		retrofit2.Response<ResponseBody> response = this.apiTriggers.spendRulesConfigure(spendRulesDTO, tenantId);
		log.info(this.objectMapper.writeValueAsString(response));
		Assertions.assertThat(response.code()).isEqualTo(200);
	}

	public void createCallBack(ZetaWebhookDto createCardResponse, String paymentId) throws IOException {
		createCallBack(createCardResponse, paymentId, true);
	}

	public void createCallBack(ZetaWebhookDto createCardResponse, String paymentId, boolean waitForCreatedStatus)
			throws IOException {
		log.info(String.valueOf(createCardResponse));
		retrofit2.Response<ResponseBody> response = this.apiTriggers.createCallBack(createCardResponse);
		try {
			Assertions.assertThat(response.code()).isEqualTo(200);
		}
		finally {
			closeResponseBodies(response);
		}
		if (waitForCreatedStatus) {
			cardStatus(paymentId, "CREATED");
		}
	}

	public void cancelCard(CancelCardDto cancelCardDto, String paymentId, int status) throws IOException {
		retrofit2.Response<ResponseBody> response = this.apiTriggers.cancelCardRequest(cancelCardDto);
		Assertions.assertThat(response.code()).isEqualTo(status);
		if (status == 200) {
			if (shouldMockZetaCallbacks()) {
				CardCreationStatusDto cardPollingDetails = awaitCardPollingDetails(paymentId);
				Assertions.assertThat(cardPollingDetails).isNotNull();
				Assertions.assertThat(cardPollingDetails.getData()).isNotNull();
				Assertions.assertThat(cardPollingDetails.getData().getCardGuid()).isNotBlank();

				CancelCardData cancelCardData = new CancelCardData();
				cancelCardData.setCardGuid(UUID.fromString(cardPollingDetails.getData().getCardGuid()));
				cancelCardData.setClientRequestId(waitForCancelRequestId(paymentId));
				cancelCardData.setReasonCode(cancelCardDto.getReasonCode());

				ZetaWebhookDto<CancelCardData> zetaWebhookDto = new ZetaWebhookDto<>(UUID.randomUUID().toString(),
						"1.0", Instant.now(), "SUCCESS", VIRTUAL_CARD_CANCEL, cancelCardData, null);
				retrofit2.Response<ResponseBody> callbackResponse = this.apiTriggers.createCallBack(zetaWebhookDto);
				Assertions.assertThat(callbackResponse.code()).isEqualTo(200);
			}
			cardStatus(paymentId, "CANCELLED");
			log.info("Card Cancelled Successfully");
		}
		else {
			log.info(this.objectMapper.writeValueAsString(response));
		}
	}

	public void resetCardRequest(ResetCardRequest resetCardRequest, String paymentId, int status) throws IOException {
		retrofit2.Response<ResponseBody> response = this.apiTriggers.resetCardRequest(resetCardRequest);
		logResponse("Reset card", response);
		Assertions.assertThat(response.code()).isEqualTo(status);
		if (status == 200) {
			handleResetSuccess(resetCardRequest, paymentId);
		}
	}

	public void resetCardRequest(ResetCardRequest resetCardRequest, String paymentId, int... expectedStatuses)
			throws IOException {
		retrofit2.Response<ResponseBody> response = this.apiTriggers.resetCardRequest(resetCardRequest);
		logResponse("Reset card", response);
		Integer[] allowedStatuses = java.util.Arrays.stream(expectedStatuses).boxed().toArray(Integer[]::new);
		Assertions.assertThat(response.code()).isIn((Object[]) allowedStatuses);
		if (response.code() == 200) {
			handleResetSuccess(resetCardRequest, paymentId);
		}
	}

	private void logResponse(String operation, Response<ResponseBody> response) throws IOException {
		String responseBody = "";
		if (response.body() != null) {
			responseBody = response.body().string();
		}
		else if (response.errorBody() != null) {
			responseBody = response.errorBody().string();
		}
		log.info("{} API response status: {}, body: {}", operation, response.code(), responseBody);
	}

	private void handleResetSuccess(ResetCardRequest resetCardRequest, String paymentId) throws IOException {
		if (shouldMockZetaCallbacks()) {
			awaitCardResetStatus(resetCardRequest.getReceiptId(), REQUEST_ACCEPTED.name());
			ObjectNode virtualCardDetails = waitForVirtualCardDetails(paymentId);
			ObjectNode webhookPayload = buildResetWebhookPayload(resetCardRequest, virtualCardDetails);
			retrofit2.Response<ResponseBody> callbackResponse = this.apiTriggers.createCallBack(webhookPayload);
			try {
				Assertions.assertThat(callbackResponse.code()).isEqualTo(200);
			}
			finally {
				closeResponseBodies(callbackResponse);
			}
		}
		awaitCardResetStatus(resetCardRequest.getReceiptId(), AUTH_RESET.name());
		log.info("Card Reset Successful");
	}

	public String generateAuthEventReceiptId(LocalDate startDate, int numberOfAuthEvents, String cardId)
			throws IOException {
		Response<ResponseBody> response = this.apiTriggers.generateAuthEvents(startDate.toString(), numberOfAuthEvents,
				cardId);
		Assertions.assertThat(response.code()).isEqualTo(200);
		Assertions.assertThat(response.body()).isNotNull();

		try (ResponseBody body = response.body()) {
			String responseBodyString = body.string();
			JsonNode responseBody = this.objectMapper.readTree(responseBodyString);
			JsonNode receiptIdsByCardId = responseBody.path("receiptIdsByCardId");
			Assertions.assertThat(receiptIdsByCardId.isObject()).isTrue();

			JsonNode receiptIds = receiptIdsByCardId.path(cardId);
			Assertions.assertThat(receiptIds.isArray()).isTrue();
			Assertions.assertThat(receiptIds)
				.as("Expected a single generated receipt id for cardId %s", cardId)
				.hasSize(1);
			String receiptId = receiptIds.get(0).asText();
			log.info("Generated auth for receiptId {}", receiptId);
			return receiptId;
		}
	}

	public void generateClearExtractByDateRange(LocalDate startDate) throws IOException {
		Response<ResponseBody> response = this.apiTriggers.generateClearExtractByDateRange(startDate.toString());
		logResponse("generateClearExtractByDateRange", response);
		Assertions.assertThat(response.code()).isEqualTo(200);
		closeResponseBodies(response);
	}

	public AuthExtract getAuthExtractRecord(String receiptId) throws IOException {
		Response<ResponseBody> response = this.apiTriggers.getAuthExtractRecord(receiptId);
		logFailureResponse("getAuthExtractRecord", response);
		Assertions.assertThat(response.code()).isEqualTo(200);
		Assertions.assertThat(response.body()).isNotNull();

		try (ResponseBody body = response.body()) {
			return this.objectMapper.readValue(body.string(), AuthExtract.class);
		}
	}

	public void createIssuingWebhook(WebhookBaseDto webhookBaseDto, int status) throws IOException {
		retrofit2.Response<ResponseBody> response = this.apiTriggers.createIssuingWebhook(webhookBaseDto);
		Assertions.assertThat(response.code()).isEqualTo(status);
	}

	private ObjectNode waitForVirtualCardDetails(String paymentId) {
		final ObjectNode[] virtualCardRef = new ObjectNode[1];
		await().atMost(Duration.ofMinutes(this.waitToLoadEntity))
			.pollInterval(Duration.ofSeconds(1))
			.untilAsserted(() -> {
				ObjectNode virtualCardDetails = fetchVirtualCardDetails(paymentId);
				Assertions.assertThat(virtualCardDetails.path("cardGuid").asText()).isNotBlank();
				virtualCardRef[0] = virtualCardDetails;
			});
		return virtualCardRef[0];
	}

	private ObjectNode fetchVirtualCardDetails(String paymentId) throws IOException {
		Response<ResponseBody> virtualCardDetailsResponse = this.apiTriggers.getVirtualCardDetails(paymentId);
		Assertions.assertThat(virtualCardDetailsResponse.code()).isEqualTo(200);
		Assertions.assertThat(virtualCardDetailsResponse.body()).isNotNull();
		return (ObjectNode) this.objectMapper.readTree(virtualCardDetailsResponse.body().string());
	}

	private ObjectNode buildResetWebhookPayload(ResetCardRequest resetCardRequest, ObjectNode virtualCardDetails) {
		ObjectNode payload = this.objectMapper.createObjectNode();
		payload.put("id", UUID.randomUUID().toString());
		payload.putNull("requestId");
		payload.put("specversion", "1.0");
		payload.putNull("version");
		payload.put("time", WEBHOOK_TIME_FORMATTER.format(Instant.now()));
		payload.put("status", "SUCCESS");
		payload.put("type", VIRTUAL_CARD_RESET);

		ObjectNode data = payload.putObject("data");
		data.put("receiptId", resetCardRequest.getReceiptId());
		data.put("cardGuid", virtualCardDetails.path("cardGuid").asText());
		data.put("resetPurpose", resetCardRequest.getResetPurpose());

		ObjectNode balanceBeforeReset = data.putObject("balanceBeforeReset");
		balanceBeforeReset.put("currency", virtualCardDetails.path("currency").asText("USD"));
		balanceBeforeReset.put("amount", toMinorUnits(virtualCardDetails.path("availableBalance").asText("0")));

		ObjectNode balanceAfterReset = data.putObject("balanceAfterReset");
		balanceAfterReset.put("currency", virtualCardDetails.path("currency").asText("USD"));
		balanceAfterReset.put("amount", toMinorUnits(virtualCardDetails.path("currentBalance").asText("0")));

		payload.putNull("error");
		return payload;
	}

	private long toMinorUnits(String amount) {
		return new BigDecimal(amount).setScale(2, RoundingMode.HALF_UP).movePointRight(2).longValueExact();
	}

	public void awaitAuthExtractRecord(String receiptId) {
		try {
			Awaitility.await()
				.atMost(Duration.ofMinutes(this.waitToLoadEntity))
				.pollInterval(Duration.ofSeconds(1))
				.until(() -> {
					Response<ResponseBody> authExtractRecord = this.apiTriggers.getAuthExtractRecord(receiptId);
					try {
						if (authExtractRecord.code() != 200 || authExtractRecord.body() == null) {
							return false;
						}
						String responseBodyString = authExtractRecord.body().string();
						return !responseBodyString.isBlank() && !"null".equalsIgnoreCase(responseBodyString.trim());
					}
					finally {
						closeResponseBodies(authExtractRecord);
					}
				});
		}
		catch (ConditionTimeoutException ex) {
			throw new ConditionTimeoutException("Auth extract record not found for receipt ID: " + receiptId, ex);
		}
	}

	public void awaitAuthExtractClearingStatus(String receiptId, String expectedClearingStatus) {
		try {
			Awaitility.await()
				.atMost(Duration.ofMinutes(this.waitToLoadEntity))
				.pollInterval(Duration.ofSeconds(1))
				.untilAsserted(() -> {
					AuthExtract authExtract = getAuthExtractRecord(receiptId);
					Assertions.assertThat(authExtract).isNotNull();
					Assertions.assertThat(authExtract.getClearingStatus()).isEqualTo(expectedClearingStatus);
				});
			log.info("Auth is cleared for receiptId {}", receiptId);
		}
		catch (ConditionTimeoutException ex) {
			throw new ConditionTimeoutException("Auth extract clearingStatus did not become " + expectedClearingStatus
					+ " for receipt ID: " + receiptId, ex);
		}
	}

	private void awaitCardResetStatus(String receiptId, String expectedStatus) {
		try {
			Awaitility.await()
				.atMost(Duration.ofMinutes(this.waitToCompleteJob))
				.pollInterval(shouldMockZetaCallbacks() ? Duration.ofSeconds(1) : this.poolTime)
				.until(() -> {
					Response<ResponseBody> cardResetDetails = this.apiTriggers.getCardResetDetails(receiptId);
					try {
						if (cardResetDetails.code() == 200 && cardResetDetails.body() != null) {
							String responseBodyString = cardResetDetails.body().string();
							JSONObject jsonObject = new JSONObject(responseBodyString);
							return expectedStatus.equalsIgnoreCase(jsonObject.optString("status"));
						}
						return false;
					}
					finally {
						closeResponseBodies(cardResetDetails);
					}
				});
		}
		catch (ConditionTimeoutException ex) {
			throw new ConditionTimeoutException(
					"Card reset status did not become " + expectedStatus + " for receipt ID: " + receiptId, ex);
		}
	}

	private void closeResponseBodies(Response<ResponseBody> response) {
		if (response.body() != null) {
			response.body().close();
		}
		if (response.errorBody() != null) {
			response.errorBody().close();
		}
	}

	public void cardStatus(String paymentId, String expectedStatus) {
		try {
			Awaitility.await()
				.atMost(Duration.ofMinutes(this.waitToLoadEntity))
				.pollInterval(this.poolTime)
				.until(() -> {
					Response<ResponseBody> virtualCardDetails = this.apiTriggers.getVirtualCardDetails(paymentId);
					if (virtualCardDetails.code() == 200 && virtualCardDetails.body() != null) {
						String responseBodyString = virtualCardDetails.body().string();
						JSONObject jsonObject = new JSONObject(responseBodyString);
						String cardStatus = jsonObject.optString("status");
						return expectedStatus.equalsIgnoreCase(cardStatus);
					}
					return false;
				});
		}
		catch (ConditionTimeoutException ex) {
			throw new ConditionTimeoutException(
					"Card " + expectedStatus + " request failed for request ID: " + paymentId, ex);
		}
	}

	public void getCancelCardDetails(String paymentId, String expectedStatus, String expectedCancelReason)
			throws IOException, JSONException {
		if (shouldMockZetaCallbacks()) {
			assertCancelCardDetails(paymentId, expectedStatus, expectedCancelReason);
			return;
		}

		await().atMost(Duration.ofMinutes(this.waitToLoadEntity))
			.pollInterval(this.poolTime)
			.untilAsserted(() -> assertCancelCardDetails(paymentId, expectedStatus, "OTHER"));
	}

	private void assertCancelCardDetails(String paymentId, String expectedStatus, String expectedCancelReason)
			throws IOException, JSONException {
		Response<ResponseBody> cancelCardDetailsResponse = apiTriggers.getCancelCardDetails(paymentId);
		Assertions.assertThat(cancelCardDetailsResponse.code()).isEqualTo(200);
		Assertions.assertThat(cancelCardDetailsResponse.body()).isNotNull();
		String responseBodyString = cancelCardDetailsResponse.body().string();
		JSONObject jsonObject = new JSONObject(responseBodyString);
		String cardStatus = jsonObject.optString("status");
		String reasonCode = jsonObject.optString("reasonCode");
		Assertions.assertThat(expectedStatus).isEqualTo(cardStatus);
		Assertions.assertThat(expectedCancelReason).isEqualTo(reasonCode);
	}

	private UUID waitForCancelRequestId(String paymentId) {
		final UUID[] cancelRequestIdRef = new UUID[1];
		await().atMost(Duration.ofMinutes(this.waitToLoadEntity)).pollInterval(this.poolTime).untilAsserted(() -> {
			Response<ResponseBody> cancelCardDetailsResponse = apiTriggers.getCancelCardDetails(paymentId);
			Assertions.assertThat(cancelCardDetailsResponse.code()).isEqualTo(200);
			Assertions.assertThat(cancelCardDetailsResponse.body()).isNotNull();
			String responseBodyString = cancelCardDetailsResponse.body().string();
			JSONObject jsonObject = new JSONObject(responseBodyString);
			Assertions.assertThat(jsonObject.optString("id")).isNotBlank();
			cancelRequestIdRef[0] = UUID.fromString(jsonObject.getString("id"));
		});
		return cancelRequestIdRef[0];
	}

	public void publishBatchEvent(String orgShortCode, String correlationId, Long paymentCount, BigDecimal amount)
			throws IOException {
		BatchEventDto batchEventDto = getBatchEventDto(orgShortCode, correlationId, paymentCount, amount);
		Response<ResponseBody> response = this.apiTriggers.publishBatchEvent(batchEventDto);
		Assertions.assertThat(response.code()).isEqualTo(200);
	}

	public void isBatchEventRegister(String correlationId) {

		try {
			Awaitility.await().atMost(Duration.ofMinutes(1)).pollInterval(this.poolTime).until(() -> {
				Response<ResponseBody> response = this.apiTriggers.getBatchEvent(correlationId);
				return response.code() == 200;
			});
		}
		catch (ConditionTimeoutException ex) {
			throw new ConditionTimeoutException("Batch Event not Register for correlationId " + correlationId + "in "
					+ this.waitToLoadEntity + " minutes ", ex);
		}
	}

	public void waitTillJobCompleted(String paymentId, String status) {
		try {
			Awaitility.await()
				.atMost(Duration.ofMinutes(this.waitToCompleteJob))
				.pollInterval(this.poolTime)
				.until(() -> {
					PaymentResponseDto paymentReponseDto = getPaymentDetails(paymentId);
					return !paymentReponseDto.getPaymentStatus().equals(status);
				});
		}
		catch (ConditionTimeoutException ex) {
			throw new ConditionTimeoutException("Payment " + paymentId + " is in status: " + status + " not completed "
					+ this.waitToCompleteJob + " minutes ", ex);
		}

	}

	public UUID loadBankAccountNonProd(String orgShortcode) throws IOException {
		BankAccountNonProd bank = getBankAccountNonProd();

		Response<ResponseBody> response = this.apiTriggers.loadBankAccountNonProd(bank, orgShortcode);
		Assertions.assertThat(response.code()).isEqualTo(200);
		String bankAccId = response.body().string().replace("\"", "");
		UUID bankAccountId = UUID.fromString(bankAccId);
		isBankAccountRegisteredInOrg(bankAccountId, orgShortcode);
		isBankAccountRegistered(bankAccountId);
		return bankAccountId;

	}

	public void isBankAccountRegisteredInOrg(UUID bankAccountId, String orgShortCode) {
		waitForEntityRegistration(() -> {
			Response<ResponseBody> bankAccountDetails = this.apiTriggers.getBankAccountDetailsFromOrg(bankAccountId,
					orgShortCode);
			return bankAccountDetails.code() == 200;
		}, this.pollTime,
				"BankAccount : " + bankAccountId + " not Registered in " + this.waitToLoadEntity + " minutes ");
	}

	private void waitForEntityRegistration(java.util.concurrent.Callable<Boolean> condition, Duration pollInterval,
			String failureMessage) {
		try {
			Awaitility.await()
				.atMost(Duration.ofMinutes(this.waitToLoadEntity))
				.pollInterval(pollInterval)
				.until(condition);
		}
		catch (ConditionTimeoutException ex) {
			throw new ConditionTimeoutException(failureMessage, ex);
		}
	}

	public void isBankAccountRegistered(UUID bankAccountId) {
		waitForEntityRegistration(() -> {
			Response<ResponseBody> bankAccountDetails = this.apiTriggers.getBankAccountDetails(bankAccountId);
			return bankAccountDetails.code() == 200;
		}, this.poolTime,
				"BankAccount : " + bankAccountId + " not Registered in " + this.waitToLoadEntity + " minutes ");
	}

	public AccountDto getAccountDetails(String orgShortCode, String accountShortCode) throws IOException {
		Response<ResponseBody> payerDetails = this.apiTriggers.getAccountDetails(orgShortCode, accountShortCode);
		Assertions.assertThat(payerDetails.code()).isEqualTo(200);
		return this.objectMapper.readValue(payerDetails.body().string(), AccountDto.class);
	}

	public SubAccountDto getSubAccountDetails(String orgShortCode, String AccountShortCode, String subAccountShortCode)
			throws IOException {
		Response<ResponseBody> subPayerDetails = this.apiTriggers.getSubAccountDetails(orgShortCode, AccountShortCode,
				subAccountShortCode);
		Assertions.assertThat(subPayerDetails.code()).isEqualTo(200);
		return this.objectMapper.readValue(subPayerDetails.body().string(), SubAccountDto.class);
	}

	public void isAccountRegistered(String accountShortCode, String orgShortcode) {
		waitForEntityRegistration(() -> {
			Response<ResponseBody> payerDetails = this.apiTriggers.getAccountDetails(orgShortcode, accountShortCode);
			return payerDetails.code() == 200;
		}, this.poolTime,
				"Account : " + accountShortCode + "  not Registered in " + this.waitToLoadEntity + " minutes ");
	}

	public void isSubAccountRegistered(String orgShortCode, String accountShortCode, String subAccountShortCode) {
		waitForEntityRegistration(() -> {
			Response<ResponseBody> subPayerDetails = this.apiTriggers.getSubAccountDetails(orgShortCode,
					accountShortCode, subAccountShortCode);
			return subPayerDetails.code() == 200;
		}, this.poolTime,
				"SubAccount : " + subAccountShortCode + "  not Registered in " + this.waitToLoadEntity + " minutes ");
	}

}
