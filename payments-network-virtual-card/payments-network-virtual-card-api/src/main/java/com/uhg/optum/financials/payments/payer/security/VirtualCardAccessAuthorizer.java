package com.uhg.optum.financials.payments.payer.security;

import com.uhg.optum.financials.shared.api.security.authorization.AccessAuthorizer;
import com.uhg.optum.financials.shared.api.security.authorization.AccessEnforcer;
import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.web.method.HandlerMethod;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.BALANCES;

@Component
@Slf4j
public class VirtualCardAccessAuthorizer implements AccessAuthorizer {

	/**
	 * Checks if the request is allowed based on the organization short code.
	 * @param accessEnforcer the access enforcer to check organization access
	 * @param request the HTTP servlet request
	 * @param handlerMethod the handler method being invoked
	 * @param args the arguments passed to the handler method
	 * @return true if the request is allowed, false otherwise
	 */
	@Override
	public boolean allowRequest(AccessEnforcer accessEnforcer, HttpServletRequest request, HandlerMethod handlerMethod,
			Object[] args) {
		String requestUrl = request.getRequestURI();
		return requestUrl.contains(BALANCES);
	}

}
