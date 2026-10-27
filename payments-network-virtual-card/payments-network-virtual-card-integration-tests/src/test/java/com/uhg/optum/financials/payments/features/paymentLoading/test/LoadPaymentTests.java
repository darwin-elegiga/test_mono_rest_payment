package com.uhg.optum.financials.payments.features.paymentLoading.test;

import com.uhg.optum.financials.payments.common.TestContext;
import com.uhg.optum.financials.payments.features.paymentLoading.implementation.LoadPaymentImpl;
import org.json.JSONException;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

import java.io.IOException;

/**
 * Description: LoadPayment Tests.
 *
 * @author p1058, created on 16-April-2024
 */
class LoadPaymentTests extends LoadPaymentImpl {

	private static TestContext testContext;

	@BeforeAll
	static void setUp() throws IOException, JSONException {
		testContext = baseSetUp();
	}

	@Tag("eph")
	@Test
	void testLoadNewCardPaymentAndValidateStatuses() throws IOException {
		super.testLoadNewCardPaymentAndValidateStatusesAcrossEndpoints();
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
