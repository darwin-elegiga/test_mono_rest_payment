package com.uhg.optum.financials.payments.payer.util;

import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.oauth2.core.oidc.user.OidcUser;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationToken;

// Utility class to extract display name from Authentication
public final class UserNameExtractUtil {

	private UserNameExtractUtil() {
		// Utility class
	}

	public static String getUserNameFromToken() {
		Authentication auth = SecurityContextHolder.getContext().getAuthentication();
		if (auth == null || !auth.isAuthenticated()) {
			return "anonymous";
		}

		// OIDC user (when using spring-security oauth2 client)
		if (auth.getPrincipal() instanceof OidcUser oidcUser) {
			String name = firstNonBlank(oidcUser.getFullName(), oidcUser.getGivenName(),
					oidcUser.getPreferredUsername(), oidcUser.getEmail());
			return (name != null) ? name : oidcUser.getSubject();
		}

		// JWT resource server authentication
		if (auth instanceof JwtAuthenticationToken jwtAuth) {
			Jwt jwt = jwtAuth.getToken();
			String name = firstNonBlank(jwt.getClaimAsString("name"), jwt.getClaimAsString("preferred_username"),
					jwt.getClaimAsString("given_name"), jwt.getClaimAsString("email"));
			return (name != null) ? name : jwt.getSubject(); // fallback to sub
		}

		return auth.getName();
	}

	private static String firstNonBlank(String... values) {
		for (String v : values) {
			if (v != null && !v.isBlank()) {
				return v;
			}
		}
		return null;
	}

}
