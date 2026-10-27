package com.uhg.optum.financials.payments.features.authClear.test;

import com.uhg.optum.financials.payments.common.TestContext;
import com.uhg.optum.financials.payments.features.authClear.implementation.AuthClearImpl;
import org.json.JSONException;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.MethodOrderer;
import org.junit.jupiter.api.Order;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.TestMethodOrder;
import org.junit.jupiter.api.parallel.Execution;
import org.junit.jupiter.api.parallel.ExecutionMode;

import java.io.IOException;
import java.time.LocalDate;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.AUTH_CLEAR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.AUTH_RESET_CLEAR;

@Execution(ExecutionMode.SAME_THREAD)
@TestMethodOrder(MethodOrderer.OrderAnnotation.class)
public class AuthClearTest extends AuthClearImpl {

	private static TestContext testContext;

	@BeforeAll
	static void setUp() throws IOException, JSONException {
		testContext = baseSetUp();
	}

	// @Tag("eph")
	@Order(1)
	@Test
	void testAuthIsMarkedAsAuthClearAfterGeneratingClearExtract() throws IOException {
		LocalDate paymentDate = LocalDate.now();
		AuthClearScenario scenario = prepareAuthorizedCard(paymentDate);

		generateClearExtractAndAssertClearingStatus(scenario.receiptId(), paymentDate, AUTH_CLEAR);
	}

	// @Tag("eph")
	@Order(2)
	@Test
	void testAfterClearResetFails() throws IOException {
		LocalDate paymentDate = LocalDate.now();
		AuthClearScenario scenario = prepareAuthorizedCard(paymentDate);

		generateClearExtractAndAssertClearingStatus(scenario.receiptId(), paymentDate, AUTH_CLEAR);
		resetCard(scenario.paymentRequestDto(), scenario.receiptId(), 400);
	}

	// @Tag("eph")
	@Order(3)
	@Test
	void testAfterResetClearShouldNotWork() throws IOException {
		LocalDate paymentDate = LocalDate.now();
		AuthClearScenario scenario = prepareAuthorizedCard(paymentDate);

		resetCard(scenario.paymentRequestDto(), scenario.receiptId(), 200);
		assertAuthExtractClearingStatus(scenario.receiptId(), AUTH_RESET_CLEAR);

		apiEndPointsImpl.generateClearExtractByDateRange(paymentDate);
		apiEndPointsImpl.awaitAuthExtractClearingStatus(scenario.receiptId(), AUTH_RESET_CLEAR);
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
