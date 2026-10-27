package com.uhg.optum.financials.payments.payer.security.route;

import jakarta.servlet.http.HttpServletRequest;

import java.util.List;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.ACCOUNT_SHORT_CODE;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.SUB_ACCOUNT_SHORT_CODE;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.X_OPTUM_TENANT_ID;

public final class RequestRouteContext {

	private RequestRouteContext() {
		// private constructor
	}

	public static List<String> fetchShortCode(HttpServletRequest request, List<String> shortCodes) {
		String tenantId = request.getHeader(X_OPTUM_TENANT_ID);
		shortCodes.set(0, tenantId);
		String accountShortCode = request.getParameter(ACCOUNT_SHORT_CODE);
		String subAccountShortCode = request.getParameter(SUB_ACCOUNT_SHORT_CODE);
		shortCodes.set(1, accountShortCode);
		shortCodes.set(2, subAccountShortCode);
		return shortCodes;
	}

}
