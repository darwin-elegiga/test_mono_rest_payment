package com.uhg.optum.financials.payments.payer.config;

import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.List;

class EventBusPropertiesTests {

	private EventBusProperties eventBusProperties;

	@BeforeEach
	void setUp() {
		this.eventBusProperties = new EventBusProperties(List.of("organizations", "payments"));
	}

	@Test
	void testEventBusTopics() {
		List<String> topics = this.eventBusProperties.topics();
		Assertions.assertThat(topics).containsExactly("organizations", "payments");
	}

}
