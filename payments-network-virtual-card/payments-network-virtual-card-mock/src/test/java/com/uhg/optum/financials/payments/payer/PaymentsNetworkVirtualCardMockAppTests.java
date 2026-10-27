package com.uhg.optum.financials.payments.payer;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.MockedStatic;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.boot.SpringApplication;

/**
 * Main application class for PaymentsNetworkVirtualCardMockAppTests.
 *
 * @author alawaniy, created on 05-June-2025
 */

@ExtendWith(MockitoExtension.class)
public class PaymentsNetworkVirtualCardMockAppTests {

	@Test
	public void testStaticMainMethod() {
		String[] args = new String[] { "args" };
		try (MockedStatic<SpringApplication> appConfig = Mockito.mockStatic(SpringApplication.class)) {
			SpringApplication.run(PaymentsNetworkVirtualCardMockApp.class, args);
			appConfig.verify(() -> PaymentsNetworkVirtualCardMockApp.main(args));
		}
	}

}
