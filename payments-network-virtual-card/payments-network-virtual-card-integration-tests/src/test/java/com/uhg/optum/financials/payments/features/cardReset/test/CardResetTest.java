package com.uhg.optum.financials.payments.features.cardReset.test;

import com.uhg.optum.financials.payments.common.TestContext;
import com.uhg.optum.financials.payments.features.cardReset.implementation.CardResetImpl;
import org.json.JSONException;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.NullSource;
import org.junit.jupiter.params.provider.ValueSource;

import java.io.IOException;

public class CardResetTest extends CardResetImpl {

	private static TestContext testContext;

	@BeforeAll
	static void setUp() throws IOException, JSONException {
		testContext = baseSetUp();
	}

	// @Tag("eph")
	// @Tag(LevelNames.GOLD)
	// @Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "Wrong Amount provided", "^%%$#" })
	void testResetTest(String resetReason) throws IOException {
		CardResetScenario scenario = prepareCardForReset();

		resetCard(scenario, resetReason, 200);
		assertAuthResetClearStatus(scenario.receiptId());
	}

	// @Tag("eph")
	// @Tag(LevelNames.GOLD)
	// @Tag(LevelNames.SILVER)
	@ParameterizedTest
	@NullSource
	@ValueSource(strings = { "" })
	void testResetWithInvalidResetReason(String resetReason) throws IOException {
		CardResetScenario scenario = prepareCardForReset();

		resetCard(scenario, resetReason, 400);
	}

	// @Tag("eph")
	// @Tag(LevelNames.GOLD)
	// @Tag(LevelNames.SILVER)
	@ParameterizedTest
	@NullSource
	@ValueSource(strings = { "", "12345", "^%%$#" })
	void testResetWithInvalidRequestId(String paymentId) throws IOException {
		CardResetScenario scenario = prepareCardForReset();

		resetCard(scenario, "test", paymentId, 400, 404);
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
