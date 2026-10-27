package com.uhg.optum.financials.payments.payer.config;

import org.springdoc.core.models.GroupedOpenApi;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnMissingBean;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class CustomSwaggerConfig {

	@Value("${application.url.base-path}")
	private String appUrlBasePath;

	@Bean
	@ConditionalOnMissingBean(name = { "apiV2" })
	public GroupedOpenApi apiV2() {
		return GroupedOpenApi.builder()
			.group("v2")
			.pathsToMatch(this.appUrlBasePath + "/cards/**", this.appUrlBasePath + "/partner-providers/**")
			.build();
	}

}
