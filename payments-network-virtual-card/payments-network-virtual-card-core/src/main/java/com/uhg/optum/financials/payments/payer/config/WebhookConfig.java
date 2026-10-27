package com.uhg.optum.financials.payments.payer.config;

import com.azure.identity.WorkloadIdentityCredentialBuilder;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.service.impl.WebhookClientImpl;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.core.config.KeyVaultConfig;
import com.uhg.optum.financials.shared.core.http.BearerTokenInterceptor;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import org.apache.commons.lang3.StringUtils;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.client.SimpleClientHttpRequestFactory;
import org.springframework.web.client.RestTemplate;

@Configuration
public class WebhookConfig {

	@Value("${azure.client.id:}")
	private String clientId;

	@Value("${azure.tenant.id:}")
	private String tenantId;

	@Value("${azure.federated.token.file:}")
	private String tokenFile;

	@Value("${spring.cloud.azure.active-directory.credential.client-id:}")
	private String appClientId;

	@Value("${keyvault.kind:Local}")
	private String kind;

	@Value("${http.max.retries:3}")
	private int maxRetries;

	@Value("${http.retry.delayInMs:1000}")
	private long retryDelayInMs;

	@Value("${http.connect.timeout.ms:0}")
	private int connectTimeoutMs;

	@Value("${http.read.timeout.ms:0}")
	private int readTimeoutMs;

	private final LogUtility logUtility;

	private final ResilientRestClientFactory resilientRestClientFactory;

	public WebhookConfig(LogUtility logUtility, ResilientRestClientFactory resilientRestClientFactory) {
		this.logUtility = logUtility;
		this.resilientRestClientFactory = resilientRestClientFactory;
	}

	@Bean("webhookRestTemplate")
	public RestTemplate restTemplate() {
		RestTemplate restTemplate = new RestTemplate();
		SimpleClientHttpRequestFactory requestFactory = new SimpleClientHttpRequestFactory();
		requestFactory.setConnectTimeout(resolveTimeout(this.connectTimeoutMs));
		requestFactory.setReadTimeout(resolveTimeout(this.readTimeoutMs));
		restTemplate.setRequestFactory(requestFactory);
		if (StringUtils.equalsIgnoreCase(this.kind, KeyVaultConfig.VaultKind.Azure.name())) {
			restTemplate.setInterceptors(java.util.List
				.of(new BearerTokenInterceptor(new WorkloadIdentityCredentialBuilder().clientId(this.clientId)
					.tenantId(this.tenantId)
					.tokenFilePath(this.tokenFile)
					.build(), this.appClientId)));
		}
		return restTemplate;
	}

	private int resolveTimeout(int configuredTimeoutMs) {
		return (configuredTimeoutMs > 0) ? configuredTimeoutMs : 30000;
	}

	@Bean
	public WebhookClient webhookService() {
		ResilientRestClient webhookClient = this.resilientRestClientFactory.getClient("webhook", (builder) -> {
			if (StringUtils.equalsIgnoreCase(this.kind, KeyVaultConfig.VaultKind.Azure.name())) {
				builder.requestInterceptor(
						new BearerTokenInterceptor(new WorkloadIdentityCredentialBuilder().clientId(this.clientId)
							.tenantId(this.tenantId)
							.tokenFilePath(this.tokenFile)
							.build(), this.appClientId));
			}
		});
		return new WebhookClientImpl(webhookClient, this.logUtility);
	}

}
