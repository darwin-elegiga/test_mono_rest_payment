package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.shared.api.config.SecurityConfig;
import com.uhg.optum.financials.shared.api.security.CommonOpenAPISecurityConfig;
import com.uhg.optum.financials.shared.api.security.CommonSecurityConfig;
import com.uhg.optum.financials.shared.api.security.RoleEnum;
import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.servers.Server;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnExpression;
import org.springframework.boot.autoconfigure.condition.ConditionalOnMissingBean;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.env.ConfigurableEnvironment;
import org.springframework.core.annotation.Order;
import org.springframework.security.config.annotation.method.configuration.EnableMethodSecurity;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.web.filter.OncePerRequestFilter;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;

import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Arrays;
import java.util.stream.Stream;

@Configuration
@ConditionalOnExpression("${server.enable.tomcat:true}")
@EnableWebSecurity
@EnableMethodSecurity
@Slf4j
public class WebSecurityConfig {

	private static final String STP_CALLBACK_WHITELIST_URL = "/cards/stp-enrollment-callback";

	private static final String STP_REMITTANCE_WHITELIST_URL = "/mock/stp/remittance/no-auth/stp-provider";

	private static final String STP_REMITTANCE_NO_OAUTH_WHITELIST_URL = "/mock/stp/remittance/auth/stp-provider1";

	private static final String STP_REMITTANCE_OAUTH_WHITELIST_URL = "/mock/stp/remittance/auth/stp-provider2";

	private static final String STP_REMITTANCE_TOKEN_WHITELIST_URL = "/mock/stp/oauth/token";

	private static final String REMITTANCE_835_PROCESS_URL = "/v2/835/process";

	@Value("${application.whitelistUrls}")
	private String[] swaggerWhiteListUrls;

	@Value("${application.bypass:false}")
	private boolean bypass;

	@Value("${application.csrf-whitelistUrls:}")
	private String[] csrfWhitelistUrls;

	@Value("${spring.cloud.azure.active-directory.scope.key}")
	private String scopeKey;

	@Value("${spring.cloud.azure.active-directory.scope.value}")
	private String scopeValue;

	@Value("${base.url}")
	private String serverURL;

	@Value("${server.servlet.context-path}")
	private String contextPath;

	@Value("${application.openapiSchema}")
	private String oauthSchema;

	@Value("${application.oauthUrl}")
	private String oauthUrl;

	@Value("${spring.cloud.azure.active-directory.profile.tenant-id}")
	private String tenantId;

	@Value("${application.tokenUrl}")
	private String tokenUrl;

	@Value("${spring.security.oidc-authorities:#{null}}")
	private Optional<String[]> oidcAuthorities;

	@Value("${spring.security.swagger-ui-scope:#{null}}")
	private Optional<String> swaggerUiScope;

	@Value("${spring.security.swagger-ui-authority:#{null}}")
	private Optional<String> swaggerUiAuthority;

	@Autowired
	private ConfigurableEnvironment environment;

	public static final Map<String, List<String>> ROLE_TO_POLICIES = Map.of(RoleEnum.ADMIN.getRole(),
			List.of(NetworksPolicy.FUNDING_ADMIN.policy, NetworksPolicy.FUNDING_READ.policy,
					NetworksPolicy.FUNDING_WRITE.policy),
			RoleEnum.READ_ONLY.getRole(), List.of(NetworksPolicy.FUNDING_READ.policy), RoleEnum.MACHINE_USER.getRole(),
			List.of(NetworksPolicy.FUNDING_ADMIN.policy, NetworksPolicy.FUNDING_READ.policy,
					NetworksPolicy.FUNDING_WRITE.policy));

	@Bean
	public OpenAPI customOpenAPI() {
		Server server = new Server();
		server.setUrl(this.serverURL + this.contextPath);
		Map<String, String> scopeMap = Map.of(this.scopeValue, this.scopeKey);
		CommonOpenAPISecurityConfig commonOpenAPISecurityConfig = CommonOpenAPISecurityConfig.builder()
			.oauthSchema(this.oauthSchema)
			.scopeMap(scopeMap)
			.tenantId(this.tenantId)
			.oauthUrl(this.oauthUrl)
			.tokenUrl(this.tokenUrl)
			.build();
		OpenAPI openAPI = commonOpenAPISecurityConfig.openAPI();
		openAPI.servers(List.of(server));
		return openAPI;
	}

	@Bean(name = "payerSecurityConfig")
	@Order(2)
	@ConditionalOnMissingBean
	public SecurityFilterChain securityFilterChain(HttpSecurity http) throws Exception {
		String[] allCsrfWhitelistUrls = Stream
			.concat(Arrays.stream(this.csrfWhitelistUrls), Stream.of(WebSecurityConfig.STP_CALLBACK_WHITELIST_URL,
					WebSecurityConfig.STP_REMITTANCE_WHITELIST_URL,
					WebSecurityConfig.STP_REMITTANCE_NO_OAUTH_WHITELIST_URL,
					WebSecurityConfig.STP_REMITTANCE_OAUTH_WHITELIST_URL,
					WebSecurityConfig.STP_REMITTANCE_TOKEN_WHITELIST_URL, WebSecurityConfig.REMITTANCE_835_PROCESS_URL))
			.distinct()
			.toArray(String[]::new);
		return new CommonSecurityConfig(this.swaggerWhiteListUrls, allCsrfWhitelistUrls).securityFilterChain(http,
				ROLE_TO_POLICIES, getPolicies(), this.bypass);
	}

	@Bean
	@Order(1)
	public SecurityFilterChain stpCallbackSecurityFilterChain(HttpSecurity http) throws Exception {
		String[] callbackWhitelistUrls = { WebSecurityConfig.STP_CALLBACK_WHITELIST_URL,
				WebSecurityConfig.STP_REMITTANCE_WHITELIST_URL, WebSecurityConfig.STP_REMITTANCE_NO_OAUTH_WHITELIST_URL,
				WebSecurityConfig.STP_REMITTANCE_OAUTH_WHITELIST_URL,
				WebSecurityConfig.STP_REMITTANCE_TOKEN_WHITELIST_URL, WebSecurityConfig.REMITTANCE_835_PROCESS_URL };
		http.securityMatcher(callbackWhitelistUrls)
			.authorizeHttpRequests((auth) -> auth.anyRequest().permitAll())
			.csrf((csrf) -> csrf.ignoringRequestMatchers(callbackWhitelistUrls))
			.addFilterBefore(new OncePerRequestFilter() {
				@Override
				protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response,
						FilterChain filterChain) throws ServletException, IOException {
					filterChain.doFilter(request, response);
				}
			}, org.springframework.security.web.access.intercept.AuthorizationFilter.class);
		return http.build();
	}

	@Bean
	public SecurityConfig securityConfig() {
		return SecurityConfig.builder()
			.securityBypass(this.bypass)
			.swaggerWhiteListUrls(this.swaggerWhiteListUrls)
			.csrfWhitelistUrls(this.csrfWhitelistUrls)
			.serverURL(this.serverURL)
			.contextPath(this.contextPath)
			.oauthSchema(this.oauthSchema)
			.tenantId(this.tenantId)
			.swaggerUiScope(this.swaggerUiScope.orElse(null))
			.swaggerUiAuthority(this.swaggerUiAuthority.orElse(null))
			.oidcAuthorities(this.oidcAuthorities.orElse(null))
			.roleToPolicyMapping(ROLE_TO_POLICIES)
			.allPolicies(getPolicies())
			.build();

	}

	/**
	 * Policies.
	 */
	@Getter
	@AllArgsConstructor
	public enum NetworksPolicy {

		/**
		 * OPTUMPAY admin.
		 */
		FUNDING_ADMIN("org:funding:admin"),

		/**
		 * OPTUMPAY read.
		 */
		FUNDING_READ("org:funding:read"),

		/**
		 * OPTUMPAY write.
		 */
		FUNDING_WRITE("org:funding:write");

		private final String policy;

	}

	static List<String> getPolicies() {
		List<String> policies = new ArrayList<>();
		for (NetworksPolicy policy : NetworksPolicy.values()) {
			policies.add(policy.policy);
		}
		return policies;
	}

}
