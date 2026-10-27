package com.uhg.optum.financials.payments.payer.config;

import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.List;

public class JobNameProviderConfigTests {

	@Test
	void testProperties() {
		List<String> name = new ArrayList<>(List.of("test"));

		JobNameProviderConfig jobNameProviderConfig = new JobNameProviderConfig(name);
		Assertions.assertThat(jobNameProviderConfig.getName()).containsExactly("test");

		name.add("other");
		Assertions.assertThat(jobNameProviderConfig.getName()).containsExactly("test");
	}

}
