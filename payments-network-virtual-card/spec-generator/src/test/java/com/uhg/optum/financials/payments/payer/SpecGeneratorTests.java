package com.uhg.optum.financials.payments.payer;

import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.MockedStatic;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
public class SpecGeneratorTests {

	private MockedStatic<com.uhg.optum.financials.shared.api.spec.SpecGenerator> mockedSpecGenerator;

	@BeforeEach
	void setUp() {
		this.mockedSpecGenerator = Mockito.mockStatic(com.uhg.optum.financials.shared.api.spec.SpecGenerator.class);
	}

	@AfterEach
	void tearDown() {
		this.mockedSpecGenerator.close();
	}

	@Test
	public void main_should_delegate_to_shared_spec_generator() {
		// Arrange: happy path - do nothing when invoked
		this.mockedSpecGenerator.when(com.uhg.optum.financials.shared.api.spec.SpecGenerator::main)
			.thenAnswer((invocation) -> null);

		// Act & Assert: should not throw
		Assertions.assertThatNoException().isThrownBy(SpecGenerator::main);
	}

}
