package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.shared.api.config.SecurityConfig;
import com.uhg.optum.financials.shared.api.security.CommonOpenAPISecurityConfig;
import com.uhg.optum.financials.shared.api.security.CommonSecurityConfig;
import com.uhg.optum.financials.shared.api.security.DiscoveryDocumentKeysEnum;
import com.uhg.optum.financials.shared.api.security.RoleEnum;
import com.uhg.optum.financials.shared.api.util.OauthUtils;
import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.servers.Server;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnExpression;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.annotation.Order;
import org.springframework.core.env.ConfigurableEnvironment;
import org.springframework.security.config.annotation.method.configuration.EnableMethodSecurity;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.oauth2.core.OAuth2AuthenticationException;
import org.springframework.security.oauth2.core.OAuth2Error;
import org.springframework.security.web.access.intercept.AuthorizationFilter;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.stream.Stream;

@Configuration
@ConditionalOnExpression("${server.enable.tomcat:true}")
@EnableWebSecurity
@EnableMethodSecurity
@Slf4j
public class WebSecurityConfig {

	@Value("${application.whitelistUrls}")
	private String[] swaggerWhiteListUrls;

	@Value("${application.bypass:false}")
	private boolean bypass;

	@Value("${application.csrf-whitelistUrls:}")
	private String[] csrfWhitelistUrls;

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

	@Value("${spring.security.swagger-ui-authority:#{null}}")
	private Optional<String> swaggerUiAuthority;

	@Value("${spring.security.swagger-ui-scope:#{null}}")
	private Optional<String> swaggerUiScope;

	private final Optional<String> oidcBackchannelAuthority;

	private String swaggerUiAuthorizationUrl = "";

	private String swaggerUiTokenUrl = "";

	private final OauthUtils oauthUtils;

	private static final String WEBHOOK_WHITELIST_URL = "/cards/webhooks";

	private static final String STP_ENROLLMENT_WHITELIST_URL = "/partner-providers/enrollments";

	private static final String STP_PAYMENT_REJECT_WHITELIST_URL = "/partner-providers/payment/reject";

	private static final String[] STP_WHITELIST_URLS = { STP_ENROLLMENT_WHITELIST_URL,
			STP_PAYMENT_REJECT_WHITELIST_URL };

	@Autowired
	private ConfigurableEnvironment environment;

	@Autowired(required = false)
	private RateLimitingFilter rateLimitingFilter;

	public WebSecurityConfig(
			@Value("${spring.security.swagger-ui-authority:#{null}}") Optional<String> swaggerUiAuthority,
			@Value("${spring.security.oidc-backchannel-authority:#{null}}") Optional<String> oidcBackchannelAuthority) {
		Optional<String> publicAuthority = Optional.ofNullable(swaggerUiAuthority).orElseGet(Optional::empty);
		this.oidcBackchannelAuthority = Optional.ofNullable(oidcBackchannelAuthority).orElseGet(Optional::empty);
		this.oauthUtils = new OauthUtils(getOidcBackchannels(publicAuthority, this.oidcBackchannelAuthority));
		this.fetchSwaggerOauthMetadata(publicAuthority);
	}

	public static final Map<String, List<String>> ROLE_TO_POLICIES = Map.of(RoleEnum.ADMIN.getRole(),
			List.of(NetworksPolicy.OPTUMPAY_ADMIN.policy, NetworksPolicy.OPTUMPAY_READ.policy,
					NetworksPolicy.OPTUMPAY_WRITE.policy),
			RoleEnum.READ_ONLY.getRole(), List.of(NetworksPolicy.OPTUMPAY_READ.policy), RoleEnum.MACHINE_USER.getRole(),
			List.of(NetworksPolicy.OPTUMPAY_ADMIN.policy, NetworksPolicy.OPTUMPAY_READ.policy,
					NetworksPolicy.OPTUMPAY_WRITE.policy));

	@Bean
	public OpenAPI customOpenAPI() {
		Server server = new Server();
		server.setUrl(this.serverURL + this.contextPath);
		Map<String, String> scopeMap = Map.of(this.swaggerUiScope.orElse(""), "");
		CommonOpenAPISecurityConfig commonOpenAPISecurityConfig = CommonOpenAPISecurityConfig.builder()
			.oauthSchema(this.oauthSchema)
			.scopeMap(scopeMap)
			.tenantId(this.tenantId)
			.oauthUrl(this.swaggerUiAuthorizationUrl)
			.tokenUrl(this.swaggerUiTokenUrl)
			.build();
		OpenAPI openAPI = commonOpenAPISecurityConfig.openAPI();
		openAPI.servers(List.of(server));
		openAPI.info(new io.swagger.v3.oas.models.info.Info().title("Payments - Virtual Card API")
			.description("API for Payments - Virtual Card operations")
			.version("1.0.0"));
		return openAPI;
	}

	/**
	 * Main Security Filter Chain that uses the common configuration. It runs with lower
	 * precedence and handles all other requests.
	 */
	@Bean(name = "VirtualCardSecurityConfig")
	@Order(2)
	public SecurityFilterChain mainSecurityFilterChain(HttpSecurity http) throws Exception {
		String[] allCsrfWhitelistUrls = Stream
			.concat(Arrays.stream(this.csrfWhitelistUrls), Stream.of(WebSecurityConfig.WEBHOOK_WHITELIST_URL))
			.distinct()
			.toArray(String[]::new);
		this.applyRateLimitingFilter(http);

		return new CommonSecurityConfig(this.swaggerWhiteListUrls, allCsrfWhitelistUrls,
				this.oidcAuthorities.orElse(null), this.oauthUtils)
			.securityFilterChain(http, ROLE_TO_POLICIES, getPolicies(), this.bypass);
	}

	@Bean
	@Order(1)
	public SecurityFilterChain stpSecurityFilterChain(HttpSecurity http) throws Exception {
		http.securityMatcher(WebSecurityConfig.STP_WHITELIST_URLS)
			.authorizeHttpRequests((auth) -> auth.anyRequest().permitAll())
			.csrf((csrf) -> csrf.ignoringRequestMatchers(WebSecurityConfig.STP_WHITELIST_URLS))
			.addFilterBefore(new OncePerRequestFilter() {
				@Override
				protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response,
						FilterChain filterChain) throws ServletException, IOException {
					filterChain.doFilter(request, response);
				}
			}, AuthorizationFilter.class);
		this.applyRateLimitingFilter(http);
		return http.build();
	}

	private void applyRateLimitingFilter(HttpSecurity http) {
		if (this.rateLimitingFilter != null) {
			http.addFilterBefore(this.rateLimitingFilter, AuthorizationFilter.class);
		}
	}

	@Bean
	public SecurityConfig securityConfig() {
		return SecurityConfig.builder()
			.securityBypass(this.bypass)
			.swaggerWhiteListUrls(this.swaggerWhiteListUrls)
			.csrfWhitelistUrls(getAllCsrfWhitelistUrls())
			.serverURL(this.serverURL)
			.contextPath(this.contextPath)
			.oauthSchema(this.oauthSchema)
			.tenantId(this.tenantId)
			.swaggerUiScope(this.swaggerUiScope.orElse(null))
			.swaggerUiAuthority(this.swaggerUiAuthority.orElse(null))
			.oidcBackchannelAuthority(this.oidcBackchannelAuthority.filter((value) -> !value.isBlank()).orElse(null))
			.oidcAuthorities(this.oidcAuthorities.orElse(null))
			.roleToPolicyMapping(ROLE_TO_POLICIES)
			.allPolicies(getPolicies())
			.build();
	}

	private String[] getAllCsrfWhitelistUrls() {
		return Stream
			.concat(Arrays.stream(this.csrfWhitelistUrls),
					Stream.concat(Stream.of(WebSecurityConfig.WEBHOOK_WHITELIST_URL),
							Arrays.stream(STP_WHITELIST_URLS)))
			.distinct()
			.toArray(String[]::new);
	}

	private static Map<String, String> getOidcBackchannels(Optional<String> authority,
			Optional<String> backchannelAuthority) {
		return backchannelAuthority.filter((value) -> !value.isBlank())
			.map((value) -> Map.of(authority.orElseThrow(
					() -> new IllegalArgumentException("OIDC authority is required when a backchannel is configured.")),
					value))
			.orElseGet(Map::of);
	}

	public void fetchSwaggerOauthMetadata(Optional<String> authority) {
		try {
			if (authority != null && authority.isPresent()) {
				String oauthMetadata = this.oauthUtils.fetchDiscoveryDocument(authority.get());
				String discoveredAuthorizationUrl = (String) this.oauthUtils.parseValueFromDiscoveryDoc(oauthMetadata,
						DiscoveryDocumentKeysEnum.AUTHORIZATION_ENDPOINT);
				this.swaggerUiAuthorizationUrl = this.oauthUtils.resolvePublicEndpoint(authority.get(),
						discoveredAuthorizationUrl);
				String discoveredTokenUrl = (String) this.oauthUtils.parseValueFromDiscoveryDoc(oauthMetadata,
						DiscoveryDocumentKeysEnum.TOKEN_ENDPOINT);
				this.swaggerUiTokenUrl = this.oauthUtils.resolvePublicEndpoint(authority.get(), discoveredTokenUrl);
			}
		}
		catch (Exception ex) {
			log.error("Error fetching oauth metadata: {}", ex.getMessage(), ex);
			OAuth2Error error = new OAuth2Error("oauth_metadata_error",
					"Failed to initialize WebSecurityConfig due to error fetching oauth metadata", null);
			throw new OAuth2AuthenticationException(error, error.getDescription(), ex);
		}
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
		OPTUMPAY_ADMIN("org:networks:admin"),

		/**
		 * OPTUMPAY read.
		 */
		OPTUMPAY_READ("org:networks:read"),

		/**
		 * OPTUMPAY write.
		 */
		OPTUMPAY_WRITE("org:networks:write");

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
