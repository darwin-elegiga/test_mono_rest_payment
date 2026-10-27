package com.uhg.optum.financials.payments.payer.config;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;

class ZetaApiPropertiesTests {

	private ZetaApiProperties zetaApiProperties;

	@BeforeEach
	void setUp() {
		this.zetaApiProperties = new ZetaApiProperties(123, "clientIdValue", "clientSecretValue", "tokenUrlValue",
				"baseUrlValue", "getCardUrlValue", "createCardUrlValue", "cancelCardUrlValue", "resetCardUrlValue",
				"activationUrlValue", "deactivationUrlValue", "healthCheckUrlValue");
	}

	@Test
	void getters_shouldReturnConstructorValues() {
		assertThat(this.zetaApiProperties.getTenant()).isEqualTo(123);
		assertThat(this.zetaApiProperties.getClientId()).isEqualTo("clientIdValue");
		assertThat(this.zetaApiProperties.getClientSecret()).isEqualTo("clientSecretValue");
		assertThat(this.zetaApiProperties.getTokenUrl()).isEqualTo("tokenUrlValue");
		assertThat(this.zetaApiProperties.getBaseUrl()).isEqualTo("baseUrlValue");
		assertThat(this.zetaApiProperties.getGetCardUrl()).isEqualTo("getCardUrlValue");
		assertThat(this.zetaApiProperties.getCreateCardUrl()).isEqualTo("createCardUrlValue");
		assertThat(this.zetaApiProperties.getCancelCardUrl()).isEqualTo("cancelCardUrlValue");
		assertThat(this.zetaApiProperties.getResetCardUrl()).isEqualTo("resetCardUrlValue");
	}

}
