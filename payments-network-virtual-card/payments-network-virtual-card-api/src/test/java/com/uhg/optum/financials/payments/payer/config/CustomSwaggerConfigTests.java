package com.uhg.optum.financials.payments.payer.config;

import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springdoc.core.models.GroupedOpenApi;
import org.springframework.test.util.ReflectionTestUtils;

@ExtendWith(MockitoExtension.class)
class CustomSwaggerConfigTests {

	@InjectMocks
	private CustomSwaggerConfig customSwaggerConfig;

	@Test
	void apiV2_returnsGroupedOpenApi() {
		ReflectionTestUtils.setField(this.customSwaggerConfig, "appUrlBasePath", "/v1");

		GroupedOpenApi groupedOpenApi = this.customSwaggerConfig.apiV2();

		Assertions.assertThat(groupedOpenApi).isNotNull();
	}

}
