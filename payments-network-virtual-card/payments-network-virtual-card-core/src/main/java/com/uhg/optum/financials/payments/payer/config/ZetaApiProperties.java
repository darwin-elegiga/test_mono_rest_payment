package com.uhg.optum.financials.payments.payer.config;

import lombok.Getter;
import org.springframework.boot.context.properties.ConfigurationProperties;

@Getter
@ConfigurationProperties(prefix = "zeta.api")
public class ZetaApiProperties {

	private final int tenant;

	// Allow empty values so BearerTokenGenerator can fall back to SecretService.
	private final String clientId;

	// Allow empty values so BearerTokenGenerator can fall back to SecretService.
	private final String clientSecret;

	private final String tokenUrl;

	private final String baseUrl;

	private final String getCardUrl;

	private final String createCardUrl;

	private final String cancelCardUrl;

	private final String resetCardUrl;

	private final String getCardBalanceUrl;

	private final String getAllRulesUrl;

	private final String getRuleByRuleIdUrl;

	public ZetaApiProperties(int tenant, String clientId, String clientSecret, String tokenUrl, String baseUrl,
			String getCardUrl, String createCardUrl, String cancelCardUrl, String resetCardUrl,
			String getCardBalanceUrl, String getAllRulesUrl, String getRuleByRuleIdUrl) {
		this.tenant = tenant;
		this.clientId = clientId;
		this.clientSecret = clientSecret;
		this.tokenUrl = tokenUrl;
		this.baseUrl = baseUrl;
		this.getCardUrl = getCardUrl;
		this.createCardUrl = createCardUrl;
		this.cancelCardUrl = cancelCardUrl;
		this.resetCardUrl = resetCardUrl;
		this.getCardBalanceUrl = getCardBalanceUrl;
		this.getAllRulesUrl = getAllRulesUrl;
		this.getRuleByRuleIdUrl = getRuleByRuleIdUrl;
	}

}
