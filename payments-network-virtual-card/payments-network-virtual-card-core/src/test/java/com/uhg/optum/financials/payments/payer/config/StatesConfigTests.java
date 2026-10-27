package com.uhg.optum.financials.payments.payer.config;

import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.HashMap;
import java.util.Map;

@ExtendWith(MockitoExtension.class)
public class StatesConfigTests {

	@InjectMocks
	private StatesConfig statesConfig;

	@Test
	void testStatesConfig() {
		Map<String, String> states = new HashMap<>();
		states.put("AL", "Alabama");
		this.statesConfig = new StatesConfig(states);
		Assertions.assertThat(this.statesConfig.getStates()).isEqualTo(states);

	}

}
