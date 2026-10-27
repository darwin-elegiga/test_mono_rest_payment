package com.uhg.optum.financials.payments.payer;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.MockedStatic;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.boot.SpringApplication;

/**
 * Main application class for PaymentsNetworkVirtualCardBootstrapAppTests.
 *
 * @author alawaniy, created on 05-June-2025
 */

@ExtendWith(MockitoExtension.class)
class PaymentsNetworkVirtualCardBootstrapAppTests {

	@InjectMocks
	private PaymentsNetworkVirtualCardBootstrapApp paymentsNetworkVirtualCardBootstrapApp;

	@Test
	void testStaticMainMethod() {
		String[] args = new String[] { "args" };
		try (MockedStatic<SpringApplication> appConfig = Mockito.mockStatic(SpringApplication.class)) {
			SpringApplication.run(PaymentsNetworkVirtualCardBootstrapApp.class, args);
			appConfig.verify(() -> this.paymentsNetworkVirtualCardBootstrapApp.main(args));
		}

	}

}
