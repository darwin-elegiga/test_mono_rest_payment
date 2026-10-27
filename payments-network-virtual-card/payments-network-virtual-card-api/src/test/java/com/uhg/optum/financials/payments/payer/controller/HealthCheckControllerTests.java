package com.uhg.optum.financials.payments.payer.controller;

import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.mockito.BDDMockito;
import org.mockito.Mockito;
import org.springframework.boot.health.actuate.endpoint.HealthEndpoint;

public class HealthCheckControllerTests {

	@Test
	void healthCheck_success() {
		// Given
		HealthEndpoint mockHealthEndpoint = Mockito.mock(HealthEndpoint.class);
		HealthCheckController controller = new HealthCheckController(mockHealthEndpoint);

		// When
		controller.healthCheck();

		// Then
		BDDMockito.then(mockHealthEndpoint).should(BDDMockito.times(1)).health();
		BDDMockito.verifyNoMoreInteractions(mockHealthEndpoint);
	}

	@Test
	void constructor_shouldInitializeHealthEndpoint() {
		// Given
		HealthEndpoint mockEndpoint = Mockito.mock(HealthEndpoint.class);

		// When
		HealthCheckController controller = new HealthCheckController(mockEndpoint);

		// Then
		Assertions.assertThat(controller).isNotNull();
	}

}
