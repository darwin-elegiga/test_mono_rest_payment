package com.uhg.optum.financials.payments.features.cardCancellation.test;

import com.uhg.optum.financials.payments.common.TestContext;
import com.uhg.optum.financials.payments.features.cardCancellation.implementation.CardCancellationImpl;
import org.json.JSONException;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.NullSource;
import org.junit.jupiter.params.provider.ValueSource;

import java.io.IOException;
import java.time.LocalDate;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.OTHER;

public class CardCancellationTests extends CardCancellationImpl {

	private static TestContext testContext;

	@BeforeAll
	static void setUp() throws IOException, JSONException {
		testContext = baseSetUp();
	}

	@Tag("eph")
	@ParameterizedTest
	@ValueSource(strings = { "OTHER" })
	void testCardCancellationFraudRisk(String cancelReason) throws IOException, JSONException {
		CardCancellationScenario scenario = prepareCardForCancellation();

		cancelCardAndAssertStatus(scenario, cancelReason);
	}

	// @Tag("eph")
	// @Test
	void testCardCancellationAfterAuthorization() throws IOException, JSONException {
		CardCancellationScenario scenario = prepareCardForCancellation();
		String paymentId = scenario.paymentRequestDto().getPaymentDetails().getId();
		String cardGuid = getCardPollingDetails(paymentId).getData().getCardGuid();
		String receiptId = apiEndPointsImpl.generateAuthEventReceiptId(LocalDate.now(), 1, cardGuid);
		apiEndPointsImpl.awaitAuthExtractRecord(receiptId);

		cancelCard(scenario, paymentId, OTHER, 400);
		apiEndPointsImpl.cardStatus(paymentId, "CREATED");
	}

	@Tag("eph")
	@NullSource
	@ParameterizedTest
	@ValueSource(strings = { "", "12345" })
	void testCardCancellationWithWrongRequestId(String paymentId) throws IOException {
		CardCancellationScenario scenario = prepareCardForCancellation();

		cancelCard(scenario, paymentId, OTHER, 400);
	}

	@Override
	protected String getOrgShortCode() {
		return testContext.orgShortCode();
	}

	@Override
	protected String getAccountShortCode() {
		return testContext.accountShortCode();
	}

	@Override
	protected String getSubAccountShortCode() {
		return testContext.subAccountShortCode();
	}

}
