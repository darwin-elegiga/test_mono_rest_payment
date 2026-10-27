package com.uhg.optum.financials.payments.payer.security;

import com.uhg.optum.financials.shared.api.security.authorization.AccessEnforcer;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.mock.web.MockHttpServletRequest;
import org.springframework.web.method.HandlerMethod;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.BALANCES;
import static org.assertj.core.api.Assertions.assertThat;

@ExtendWith(MockitoExtension.class)
class VirtualCardAccessAuthorizerTests {

	private final VirtualCardAccessAuthorizer authorizer = new VirtualCardAccessAuthorizer();

	@Test
	void allowRequest_returnsTrue_whenUrlContainsBalances() {
		AccessEnforcer enforcer = Mockito.mock(AccessEnforcer.class);
		MockHttpServletRequest request = new MockHttpServletRequest();
		request.setRequestURI("/api/" + BALANCES);
		HandlerMethod handlerMethod = Mockito.mock(HandlerMethod.class);

		boolean result = this.authorizer.allowRequest(enforcer, request, handlerMethod, new Object[] {});

		assertThat(result).isTrue();
	}

	@Test
	void allowRequest_returnsFalse_whenUrlDoesNotContainBalances() {
		AccessEnforcer enforcer = Mockito.mock(AccessEnforcer.class);
		MockHttpServletRequest request = new MockHttpServletRequest();
		request.setRequestURI("/api/other");
		HandlerMethod handlerMethod = Mockito.mock(HandlerMethod.class);

		boolean result = this.authorizer.allowRequest(enforcer, request, handlerMethod, new Object[] {});

		assertThat(result).isFalse();
	}

}
