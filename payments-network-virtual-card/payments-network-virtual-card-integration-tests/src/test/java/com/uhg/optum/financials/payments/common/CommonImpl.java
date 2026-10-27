package com.uhg.optum.financials.payments.common;

import com.fasterxml.jackson.databind.node.ObjectNode;
import com.uhg.optum.financials.payments.APITrigger.APIEndPointsImpl;
import com.uhg.optum.financials.payments.APITrigger.APITriggers;
import com.uhg.optum.financials.payments.payer.dto.CardCreationStatusDto;
import com.uhg.optum.financials.payments.payer.dto.PaymentResponseDto;
import com.uhg.optum.financials.payments.payer.dto.SpendRulesDTO;
import com.uhg.optum.financials.payments.payer.dto.callback.CreateCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.utilites.DTOUtility;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import org.assertj.core.api.Assertions;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.math.BigDecimal;
import java.math.RoundingMode;
import java.time.Duration;
import java.time.Instant;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.ThreadLocalRandom;
import java.util.concurrent.TimeUnit;

import static com.uhg.optum.financials.payments.payer.constant.EventTypeConstants.VIRTUAL_CARD_CREATE;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.CARD_ISSUED;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_ACCEPTED;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_INITIATED;
import static com.uhg.optum.financials.payments.APITrigger.RetrofitClient.shouldMockZetaCallbacks;
import static org.awaitility.Awaitility.await;

public class CommonImpl extends BaseIntegrationTest {

	private static final Logger log = LoggerFactory.getLogger(APITriggers.class);

	final int waitToLoadEntity = 6;

	protected static APIEndPointsImpl apiEndPointsImpl = APIEndPointsImpl.getAPIEndPointsImplInstance();

	Duration poolTime = Duration.ofMillis(250);

	public PaymentRequestDto loadPayment(String orgShortCode, String accountShortCode, String subAccountShortCode)
			throws IOException {
		DTOUtility dtoUtility = new DTOUtility();
		PaymentRequestDto paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode,
				subAccountShortCode);
		apiEndPointsImpl.loadPayment(paymentRequestDto);
		return paymentRequestDto;
	}

	@Override
	protected String getOrgShortCode() {
		throw new UnsupportedOperationException("Subclasses must provide orgShortCode");
	}

	@Override
	protected String getAccountShortCode() {
		throw new UnsupportedOperationException("Subclasses must provide accountShortCode");
	}

	@Override
	protected String getSubAccountShortCode() {
		throw new UnsupportedOperationException("Subclasses must provide subAccountShortCode");
	}

	public CardCreation getCardCreationDetails(String requestId) throws IOException {
		return apiEndPointsImpl.getCardCreationDetails(requestId);
	}

	public CardCreationStatusDto getCardPollingDetails(String paymentId) throws IOException {
		return apiEndPointsImpl.awaitCardPollingDetails(paymentId);
	}

	public ZetaWebhookDto createZetaWebhookDto(UUID requestId, BigDecimal amount, List<String> spendRules) {
		return createZetaWebhookDto(requestId, UUID.randomUUID(), amount, spendRules);
	}

	public ZetaWebhookDto createZetaWebhookDto(UUID requestId, UUID cardGuid, BigDecimal amount,
			List<String> spendRules) {
		CreateCardData data = new CreateCardData();
		data.setClientRequestId(requestId);
		data.setCardGuid(cardGuid);
		data.setSpendRules(spendRules);
		CreateCardData.InitialBalance availableBalance = new CreateCardData.InitialBalance();
		availableBalance.setAmount(amount.setScale(2, RoundingMode.HALF_UP).movePointRight(2).longValueExact());
		availableBalance.setCurrency("USD");
		data.setInitialBalance(availableBalance);
		Instant currentTime = Instant.now();
		String idempotencyKey = "idempotency-key-" + ThreadLocalRandom.current().nextInt(1000, 10000);
		ZetaWebhookDto zetaWebhookDto = new ZetaWebhookDto(idempotencyKey, "1.0", currentTime, "SUCCESS",
				VIRTUAL_CARD_CREATE, data, null);
		return zetaWebhookDto;
	}

	protected PaymentRequestDto prepareCardFlow(String orgShortCode, String accountShortCode,
			String subAccountShortCode) throws IOException {

		PaymentRequestDto paymentRequestDto = loadPayment(orgShortCode, accountShortCode, subAccountShortCode);

		CardCreation cardCreation = waitForCardCreationDetails(paymentRequestDto.getPaymentDetails().getId());

		Assertions.assertThat(cardCreation.getStatus())
			.as("Card creation status should be either REQUEST_INITIATED or REQUEST_ACCEPTED")
			.isIn(REQUEST_INITIATED.name(), REQUEST_ACCEPTED.name());

		log.info("Card creation request initiated successfully");

		if (shouldMockZetaCallbacks()) {
			processEphCardFlow(paymentRequestDto);
		}
		else {
			waitForCardIssued(paymentRequestDto.getPaymentDetails().getId());
		}
		return paymentRequestDto;
	}

	private void processEphCardFlow(PaymentRequestDto paymentRequestDto) throws IOException {
		String paymentId = paymentRequestDto.getPaymentDetails().getId();

		CardCreationStatusDto cardPollingDetails = waitForCardPollingDetails(paymentId);
		String cardGuid = cardPollingDetails.getData().getCardGuid();
		log.info("Card polling returned cardGuid: {}", cardGuid);

		CardCreation cardCreationDetails = waitForCardCreationDetails(paymentId);
		UUID clientRequestId = cardCreationDetails.getId();
		log.info("Creation details returned clientRequestId: {}", clientRequestId);

		ZetaWebhookDto zetaWebhookDto = createZetaWebhookDto(clientRequestId, UUID.fromString(cardGuid),
				paymentRequestDto.getPaymentDetails().getAmount(), cardPollingDetails.getData().getSpendRules());
		apiEndPointsImpl.createCallBack(zetaWebhookDto, paymentId, false);

		waitForCardIssued(paymentId);
	}

	private void waitForCardIssued(String paymentId) {
		await().atMost(waitToLoadEntity, TimeUnit.MINUTES).pollInterval(10, TimeUnit.SECONDS).untilAsserted(() -> {
			CardCreation updatedCardCreationStatus = getCardCreationDetails(paymentId);
			Assertions.assertThat(updatedCardCreationStatus.getStatus()).isEqualTo(CARD_ISSUED.name());
			log.info("Card Issued");
		});
	}

	private CardCreationStatusDto waitForCardPollingDetails(String paymentId) {
		return apiEndPointsImpl.awaitCardPollingDetails(paymentId);
	}

	private CardCreation waitForCardCreationDetails(String paymentId) {
		final CardCreation[] cardCreationRef = new CardCreation[1];
		await().atMost(20, TimeUnit.SECONDS).pollInterval(4, TimeUnit.SECONDS).untilAsserted(() -> {
			CardCreation cardCreation = getCardCreationDetails(paymentId);
			Assertions.assertThat(cardCreation).isNotNull();
			Assertions.assertThat(cardCreation.getId()).isNotNull();
			cardCreationRef[0] = cardCreation;
			log.info("Card creation details available for paymentId {}", paymentId);
		});
		return cardCreationRef[0];
	}

	private SpendRulesDTO configureSpendRules(String orgShortCode, String accountShortCode, String subAccountShortCode)
			throws IOException {
		SpendRulesDTO spendRulesDTO = new SpendRulesDTO();
		spendRulesDTO.getAccounts().getFirst().setAccShortCd(accountShortCode);
		spendRulesDTO.getAccounts().getFirst().setSubAccounts(Collections.singletonList(subAccountShortCode));
		List<String> spendRules = new ArrayList<>();
		spendRules.add("RS1-1005");
		spendRules.add("RS1-1025");
		spendRulesDTO.setSpendRules(spendRules);

		apiEndPointsImpl.configureSpendRules(spendRulesDTO, orgShortCode);
		return spendRulesDTO;
	}

}
