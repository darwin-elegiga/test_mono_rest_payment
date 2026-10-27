package com.uhg.optum.financials.payments.payer.util;

import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.oauth2.core.oidc.user.OidcUser;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationToken;

@ExtendWith(MockitoExtension.class)
class UserNameExtractUtilTests {

	@AfterEach
	void tearDown() {
		SecurityContextHolder.clearContext();
	}

	@Test
	void testGetUserNameFromToken_Anonymous() {
		SecurityContextHolder.clearContext();
		Assertions.assertThat(UserNameExtractUtil.getUserNameFromToken()).isEqualTo("anonymous");
	}

	@Test
	void testGetUserNameFromToken_OidcUser() {
		OidcUser oidcUser = BDDMockito.mock(OidcUser.class);
		BDDMockito.when(oidcUser.getFullName()).thenReturn("John Doe");
		BDDMockito.when(oidcUser.getGivenName()).thenReturn("John");
		BDDMockito.when(oidcUser.getPreferredUsername()).thenReturn("jdoe");
		BDDMockito.when(oidcUser.getEmail()).thenReturn("john.doe@example.com");

		Authentication auth = BDDMockito.mock(Authentication.class);
		BDDMockito.when(auth.isAuthenticated()).thenReturn(true);
		BDDMockito.when(auth.getPrincipal()).thenReturn(oidcUser);

		SecurityContextHolder.getContext().setAuthentication(auth);

		Assertions.assertThat(UserNameExtractUtil.getUserNameFromToken()).isEqualTo("John Doe");
	}

	@Test
	void testGetUserNameFromToken_OidcUserFallback() {
		OidcUser oidcUser = BDDMockito.mock(OidcUser.class);
		BDDMockito.when(oidcUser.getFullName()).thenReturn("");
		BDDMockito.when(oidcUser.getGivenName()).thenReturn("");
		BDDMockito.when(oidcUser.getPreferredUsername()).thenReturn("");
		BDDMockito.when(oidcUser.getEmail()).thenReturn("");
		BDDMockito.when(oidcUser.getSubject()).thenReturn("sub123");

		Authentication auth = BDDMockito.mock(Authentication.class);
		BDDMockito.when(auth.isAuthenticated()).thenReturn(true);
		BDDMockito.when(auth.getPrincipal()).thenReturn(oidcUser);

		SecurityContextHolder.getContext().setAuthentication(auth);

		Assertions.assertThat(UserNameExtractUtil.getUserNameFromToken()).isEqualTo("sub123");
	}

	@Test
	void testGetUserNameFromToken_JwtAuthenticationToken() {
		Jwt jwt = BDDMockito.mock(Jwt.class);
		BDDMockito.when(jwt.getClaimAsString("name")).thenReturn("Jane Doe");
		BDDMockito.when(jwt.getClaimAsString("preferred_username")).thenReturn("janed");
		BDDMockito.when(jwt.getClaimAsString("given_name")).thenReturn("Jane");
		BDDMockito.when(jwt.getClaimAsString("email")).thenReturn("jane.doe@example.com");

		JwtAuthenticationToken jwtAuth = BDDMockito.mock(JwtAuthenticationToken.class);
		BDDMockito.when(jwtAuth.isAuthenticated()).thenReturn(true);
		BDDMockito.when(jwtAuth.getToken()).thenReturn(jwt);

		SecurityContextHolder.getContext().setAuthentication(jwtAuth);

		Assertions.assertThat(UserNameExtractUtil.getUserNameFromToken()).isEqualTo("Jane Doe");
	}

	@Test
	void testGetUserNameFromToken_JwtAuthenticationTokenFallback() {
		Jwt jwt = BDDMockito.mock(Jwt.class);
		BDDMockito.when(jwt.getClaimAsString("name")).thenReturn("");
		BDDMockito.when(jwt.getClaimAsString("preferred_username")).thenReturn("");
		BDDMockito.when(jwt.getClaimAsString("given_name")).thenReturn("");
		BDDMockito.when(jwt.getClaimAsString("email")).thenReturn("");
		BDDMockito.when(jwt.getSubject()).thenReturn("sub456");

		JwtAuthenticationToken jwtAuth = BDDMockito.mock(JwtAuthenticationToken.class);
		BDDMockito.when(jwtAuth.isAuthenticated()).thenReturn(true);
		BDDMockito.when(jwtAuth.getToken()).thenReturn(jwt);

		SecurityContextHolder.getContext().setAuthentication(jwtAuth);

		Assertions.assertThat(UserNameExtractUtil.getUserNameFromToken()).isEqualTo("sub456");
	}

	@Test
	void testGetUserNameFromToken_Default() {
		Authentication auth = BDDMockito.mock(Authentication.class);
		BDDMockito.when(auth.isAuthenticated()).thenReturn(true);
		BDDMockito.when(auth.getPrincipal()).thenReturn("principal");
		BDDMockito.when(auth.getName()).thenReturn("defaultUser");

		SecurityContextHolder.getContext().setAuthentication(auth);

		Assertions.assertThat(UserNameExtractUtil.getUserNameFromToken()).isEqualTo("defaultUser");
	}

}
