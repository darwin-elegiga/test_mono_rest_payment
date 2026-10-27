package com.uhg.optum.financials.payments.payer.config;

import static org.assertj.core.api.BDDAssertions.then;

import com.uhg.optum.financials.shared.api.config.SecurityConfig;
import com.uhg.optum.financials.shared.api.security.RoleEnum;
import io.swagger.v3.oas.models.OpenAPI;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.test.util.ReflectionTestUtils;

import java.util.List;
import java.util.Map;
import java.util.Optional;

@ExtendWith(MockitoExtension.class)
class WebSecurityConfigTests {

	@InjectMocks
	private WebSecurityConfig webSecurityConfig;

	@Test
	void testCustomOpenAPI() {
		ReflectionTestUtils.setField(this.webSecurityConfig, "serverURL", "http://localhost");
		ReflectionTestUtils.setField(this.webSecurityConfig, "contextPath", "/api");
		ReflectionTestUtils.setField(this.webSecurityConfig, "scopeKey", "scope-key");
		ReflectionTestUtils.setField(this.webSecurityConfig, "scopeValue", "scope-value");
		ReflectionTestUtils.setField(this.webSecurityConfig, "oauthSchema", "oauth-schema");
		ReflectionTestUtils.setField(this.webSecurityConfig, "oauthUrl", "https://oauth.example.com");
		ReflectionTestUtils.setField(this.webSecurityConfig, "tokenUrl", "https://token.example.com");
		ReflectionTestUtils.setField(this.webSecurityConfig, "tenantId", "tenant-id");
		OpenAPI openAPI = this.webSecurityConfig.customOpenAPI();
		then(openAPI).isNotNull();
		then(openAPI.getServers().getFirst().getUrl()).isEqualTo("http://localhost/api");
	}

	@Test
	void testSecurityConfig() {
		WebSecurityConfig configInstance = new WebSecurityConfig();
		ReflectionTestUtils.setField(configInstance, "bypass", true);
		ReflectionTestUtils.setField(configInstance, "swaggerWhiteListUrls", new String[] { "/swagger" });
		ReflectionTestUtils.setField(configInstance, "csrfWhitelistUrls", new String[] { "/csrf" });
		ReflectionTestUtils.setField(configInstance, "serverURL", "http://localhost");
		ReflectionTestUtils.setField(configInstance, "contextPath", "/api");
		ReflectionTestUtils.setField(configInstance, "oauthSchema", "oauth-schema");
		ReflectionTestUtils.setField(configInstance, "tenantId", "tenant-id");
		// Set Optional fields as Optionals, to match field type
		ReflectionTestUtils.setField(configInstance, "swaggerUiScope", Optional.of("scope"));
		ReflectionTestUtils.setField(configInstance, "oidcAuthorities", Optional.of(new String[] { "auth1", "auth2" }));
		ReflectionTestUtils.setField(configInstance, "swaggerUiAuthority", Optional.of("authority"));
		SecurityConfig config = configInstance.securityConfig();
		then(config).isNotNull();
		then(config.isSecurityBypass()).isTrue();
		then(config.getSwaggerWhiteListUrls()[0]).isEqualTo("/swagger");
		then(config.getCsrfWhitelistUrls()[0]).isEqualTo("/csrf");
		then(config.getServerURL()).isEqualTo("http://localhost");
		then(config.getContextPath()).isEqualTo("/api");
	}

	@Test
	void testGetPolicies() {
		List<String> policies = WebSecurityConfig.getPolicies();
		then(policies).contains("org:funding:admin", "org:funding:read", "org:funding:write");
	}

	@Test
	void testRoleToPoliciesMapping() {
		Map<String, List<String>> mapping = WebSecurityConfig.ROLE_TO_POLICIES;
		then(mapping.get(RoleEnum.ADMIN.getRole())).contains("org:funding:admin", "org:funding:read",
				"org:funding:write");
		then(mapping.get(RoleEnum.READ_ONLY.getRole())).contains("org:funding:read");
		then(mapping.get(RoleEnum.MACHINE_USER.getRole())).contains("org:funding:admin", "org:funding:read",
				"org:funding:write");
	}

}
