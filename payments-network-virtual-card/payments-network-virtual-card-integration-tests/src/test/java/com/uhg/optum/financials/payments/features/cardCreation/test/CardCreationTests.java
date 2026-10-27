package com.uhg.optum.financials.payments.features.cardCreation.test;

import com.uhg.optum.financials.payments.common.TestContext;
import com.uhg.optum.financials.payments.features.cardCreation.implementation.CardCreationImpl;
import org.json.JSONException;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.io.IOException;

public class CardCreationTests extends CardCreationImpl {

	private static TestContext testContext;

	@BeforeAll
	static void setUp() throws IOException, JSONException {
		testContext = baseSetUp();
	}

	// @Tag("eph")
	@Test
	void testCardCreations() throws IOException {
		var paymentRequestDto = prepareCardFlow(getOrgShortCode(), getAccountShortCode(), getSubAccountShortCode());
		publishBatchEventAndValidatePaymentCompleted(paymentRequestDto);
		validatePaymentFilesCreated(paymentRequestDto.getCorrelationId());
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
