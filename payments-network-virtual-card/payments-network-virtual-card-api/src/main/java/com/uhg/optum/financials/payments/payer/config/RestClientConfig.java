
package com.uhg.optum.financials.payments.payer.config;

import org.apache.hc.client5.http.config.RequestConfig;
import org.apache.hc.client5.http.impl.classic.CloseableHttpClient;
import org.apache.hc.client5.http.impl.classic.HttpClients;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.client.HttpComponentsClientHttpRequestFactory;
import org.springframework.web.client.RestClient;
import java.util.concurrent.TimeUnit;

@Configuration
public class RestClientConfig {

	@Bean
	public RestClient restClient() {

		RequestConfig config = RequestConfig.custom()
			.setConnectTimeout(10, TimeUnit.SECONDS) // Replacement for
														// setConnectTimeout()
			.setResponseTimeout(30, TimeUnit.SECONDS) // Replacement for setReadTimeout()
			.build();

		CloseableHttpClient client = HttpClients.custom().setDefaultRequestConfig(config).build();

		HttpComponentsClientHttpRequestFactory factory = new HttpComponentsClientHttpRequestFactory(client);

		return RestClient.builder().requestFactory(factory).build();
	}

}
