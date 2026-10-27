package com.uhg.optum.financials.payments.common;

import com.uhg.optum.financials.payments.APITrigger.APIEndPointsImpl;
import com.uhg.optum.financials.payments.utilites.DTOUtility;
import org.json.JSONException;

import java.io.IOException;

public abstract class BaseIntegrationTest extends DTOUtility {

	protected static final APIEndPointsImpl apiEndPointsImpl = APIEndPointsImpl.getAPIEndPointsImplInstance();

	protected static TestContext baseSetUp() throws IOException, JSONException {
		return TestSetupHelper.initializeTestEnvironment(apiEndPointsImpl);
	}

	protected abstract String getOrgShortCode();

	protected abstract String getAccountShortCode();

	protected abstract String getSubAccountShortCode();

}
