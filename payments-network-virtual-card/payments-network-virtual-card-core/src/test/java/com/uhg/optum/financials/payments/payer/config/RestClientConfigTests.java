package com.uhg.optum.financials.payments.payer.config;

import org.junit.jupiter.api.Test;
import org.springframework.web.client.RestClient;

import static org.assertj.core.api.Assertions.assertThat;

class RestClientConfigTests {

	private final RestClientConfig restClientConfig = new RestClientConfig();

	@Test
	void restClient_shouldReturnRestClientInstance() {
		RestClient client = this.restClientConfig.restClient();
		assertThat(client).isNotNull();
		assertThat(client).isInstanceOf(RestClient.class);
	}

}
