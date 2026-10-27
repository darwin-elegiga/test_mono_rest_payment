package com.uhg.optum.financials.payments.payer.config;

import lombok.RequiredArgsConstructor;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.client.RestClient;

@Configuration
@EnableCaching
@RequiredArgsConstructor
public class RestClientConfig {

	@Bean
	public RestClient restClient() {
		return RestClient.builder().build();
	}

}
