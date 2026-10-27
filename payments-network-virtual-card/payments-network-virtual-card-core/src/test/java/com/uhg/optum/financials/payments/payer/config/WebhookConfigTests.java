package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.test.util.ReflectionTestUtils;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.mock;

class WebhookConfigTests {

	private WebhookConfig webhookConfig;

	@BeforeEach
	void setUp() {
		LogUtility logUtility = mock(LogUtility.class);
		ResilientRestClientFactory resilientRestClientFactory = mock(ResilientRestClientFactory.class);
		this.webhookConfig = new WebhookConfig(logUtility, resilientRestClientFactory);
	}

	@Test
	void webhookService_shouldReturnWebhookClientInstance() throws Exception {
		ReflectionTestUtils.setField(this.webhookConfig, "kind", "Local");
		WebhookClient client = this.webhookConfig.webhookService();
		assertThat(client).isNotNull();
		assertThat(client).isInstanceOf(WebhookClient.class);
	}

}
