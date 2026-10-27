package com.uhg.optum.financials.payments.common;

import com.uhg.optum.financials.payments.APITrigger.APIEndPointsImpl;
import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationRegisteredEvent;
import org.json.JSONException;

import java.io.IOException;
import java.util.HashMap;
import java.util.Map;
import java.util.UUID;

import static com.uhg.optum.financials.payments.APITrigger.RetrofitClient.getEnv;

public final class TestSetupHelper {

	private static final Object TEST_CONTEXT_LOCK = new Object();

	private static final Map<String, TestContext> TEST_CONTEXTS = new HashMap<>();

	private TestSetupHelper() {
	}

	public static TestContext initializeTestEnvironment(APIEndPointsImpl apiEndPointsImpl)
			throws IOException, JSONException {
		String cacheKey = getSetupCacheKey();
		synchronized (TEST_CONTEXT_LOCK) {
			TestContext testContext = TEST_CONTEXTS.get(cacheKey);
			if (testContext == null) {
				testContext = createTestEnvironment(apiEndPointsImpl);
				TEST_CONTEXTS.put(cacheKey, testContext);
			}
			return testContext;
		}
	}

	private static TestContext createTestEnvironment(APIEndPointsImpl apiEndPointsImpl)
			throws IOException, JSONException {
		UUID orgId = null;
		UUID bankAccountId = null;
		String orgShortCode;
		String accountShortCode;
		String subAccountShortCode;

		if ("eph".equals(getEnv())) {
			OrganizationRegisteredEvent organizationEventDto = apiEndPointsImpl.loadOrganizationInVirtualCard();
			orgId = organizationEventDto.getId();
			orgShortCode = organizationEventDto.getOrganizationRegistered().getShortCode();
			accountShortCode = apiEndPointsImpl.loadAccountInVirtualCard(orgId, orgShortCode)
				.getAccountRegistered()
				.getShortCode();
			bankAccountId = apiEndPointsImpl.loadBankAccountInVirtualCard(orgId).getId();
			subAccountShortCode = apiEndPointsImpl
				.loadSubAccountInVirtualCard(orgId, orgShortCode, accountShortCode, bankAccountId)
				.getSubAccountRegistered()
				.getShortCode();
			apiEndPointsImpl.loadSubAccountUpdatedInVirtualCard(orgId, orgShortCode, accountShortCode, bankAccountId,
					subAccountShortCode);
		}
		else {
			orgShortCode = apiEndPointsImpl.loadOrganizationFromOrg();
			accountShortCode = apiEndPointsImpl.loadAccountFromOrg(orgShortCode).getShortCode();
			bankAccountId = apiEndPointsImpl.loadBankAccountNonProd(orgShortCode);
			subAccountShortCode = apiEndPointsImpl.loadSubAccountNonProd(orgShortCode, bankAccountId, accountShortCode);
			apiEndPointsImpl.loadSubAccountUpdatedNonProd(orgShortCode, bankAccountId, accountShortCode,
					subAccountShortCode);
			orgId = apiEndPointsImpl.getOrganizationIdByShortCode(orgShortCode);
		}
		if ("eph".equals(getEnv()) || "tst".equals(getEnv()) || "dev".equals(getEnv())) {
			apiEndPointsImpl.loadLogoUpdatedEventInVirtualCard(orgId, orgShortCode, accountShortCode,
					subAccountShortCode);
			apiEndPointsImpl.getLogoDetails(orgShortCode, accountShortCode, subAccountShortCode);
		}

		return new TestContext(orgId, orgShortCode, accountShortCode, subAccountShortCode, bankAccountId);
	}

	private static String getSetupCacheKey() {
		return String.valueOf(System.getProperty("baseURL")) + "|" + String.valueOf(getEnv());
	}

}
