package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.shared.api.config.SecurityConfig;
import com.uhg.optum.financials.shared.api.security.CommonSecurityConfig;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.test.util.ReflectionTestUtils;

import java.util.List;
import java.util.Optional;

import static com.uhg.optum.financials.payments.payer.config.WebSecurityConfig.getPolicies;

@ExtendWith(MockitoExtension.class)
class WebSecurityConfigTests {

	@InjectMocks
	private WebSecurityConfig webSecurityConfig;

	@Mock
	CommonSecurityConfig commonSecurityConfig;

	@Test
	void getPolicies_shouldReturnAllPolicies() {
		List<String> policies = getPolicies();

		Assertions.assertThat(policies).isNotNull();
		Assertions.assertThat(policies.size()).isEqualTo(3);
		Assertions.assertThat(policies).contains("org:networks:admin", "org:networks:read", "org:networks:write");
	}

	@Test
	void testSecurityConfig() {
		ReflectionTestUtils.setField(this.webSecurityConfig, "swaggerWhiteListUrls", new String[] {});
		ReflectionTestUtils.setField(this.webSecurityConfig, "bypass", true);
		ReflectionTestUtils.setField(this.webSecurityConfig, "csrfWhitelistUrls", new String[] {});
		ReflectionTestUtils.setField(this.webSecurityConfig, "serverURL", "1");
		ReflectionTestUtils.setField(this.webSecurityConfig, "contextPath", "1");
		ReflectionTestUtils.setField(this.webSecurityConfig, "oauthSchema", "1");
		ReflectionTestUtils.setField(this.webSecurityConfig, "oauthUrl", "1");
		ReflectionTestUtils.setField(this.webSecurityConfig, "tenantId", "1");
		ReflectionTestUtils.setField(this.webSecurityConfig, "tokenUrl", "1");
		ReflectionTestUtils.setField(this.webSecurityConfig, "oidcAuthorities",
				Optional.of(new String[] { "https://authority1.com", "https://authority2.com" }));
		ReflectionTestUtils.setField(this.webSecurityConfig, "swaggerUiScope", Optional.of("read"));
		ReflectionTestUtils.setField(this.webSecurityConfig, "swaggerUiAuthority", Optional.of("http://authority.url"));
		SecurityConfig securityConfig = this.webSecurityConfig.securityConfig();
		Assertions.assertThat(securityConfig).isNotNull();
	}

	@Test
	void testCustomOpenAPI() {
		ReflectionTestUtils.setField(this.webSecurityConfig, "serverURL", "1");
		ReflectionTestUtils.setField(this.webSecurityConfig, "contextPath", "1");
		Optional<String> swaggerUiScope = Optional.of("openid email api://92b999ca-7964-4da6-b21b-56f8ca0e7922/admin");
		ReflectionTestUtils.setField(this.webSecurityConfig, "swaggerUiScope", swaggerUiScope);
		var result = this.webSecurityConfig.customOpenAPI();
		Assertions.assertThat(result).isNotNull();
	}

	@Test
	void getAllCsrfWhitelistUrls_shouldIncludeWebhookAndStpEndpoints() {
		ReflectionTestUtils.setField(this.webSecurityConfig, "csrfWhitelistUrls", new String[] { "/v1/**" });

		String[] result = ReflectionTestUtils.invokeMethod(this.webSecurityConfig, "getAllCsrfWhitelistUrls");

		Assertions.assertThat(result)
			.containsExactlyInAnyOrder("/v1/**", "/cards/webhooks", "/partner-providers/enrollments",
					"/partner-providers/payment/reject");
	}

	@Test
	void testMainSecurityFilterChain() throws Exception {
		ReflectionTestUtils.setField(this.webSecurityConfig, "swaggerWhiteListUrls", new String[] {});
		ReflectionTestUtils.setField(this.webSecurityConfig, "bypass", false);
		ReflectionTestUtils.setField(this.webSecurityConfig, "csrfWhitelistUrls", new String[] {});
		Optional<String[]> oidcAuthorities = Optional
			.of(new String[] { "https://login.microsoftonline.com/db05faca-c82a-4b9d-b9c5-0f64b6755421/v2.0" });
		ReflectionTestUtils.setField(this.webSecurityConfig, "oidcAuthorities", oidcAuthorities);
		HttpSecurity http = Mockito.mock(HttpSecurity.class);
		Mockito.when(http.csrf(Mockito.any())).thenReturn(http);
		Mockito.when(http.oauth2ResourceServer(Mockito.any())).thenReturn(http);
		Mockito.when(http.authorizeHttpRequests(Mockito.any())).thenReturn(http);
		SecurityFilterChain filterChain = Mockito.mock(SecurityFilterChain.class);
		var result = this.webSecurityConfig.mainSecurityFilterChain(http);
		Assertions.assertThat(result).isNull();

	}

}
