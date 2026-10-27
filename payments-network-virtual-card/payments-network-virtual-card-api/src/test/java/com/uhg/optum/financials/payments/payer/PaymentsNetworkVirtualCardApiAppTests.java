package com.uhg.optum.financials.payments.payer;

import com.uhg.optum.financials.shared.core.encryption.AssymetricEncryptionService;
import com.uhg.optum.financials.shared.core.encryption.impl.ECCEncryptionService;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.MockedStatic;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.boot.SpringApplication;

/**
 * Test application class for PaymentsNetworkVirtualCardApiAppTests.
 *
 * @author alawaniy, created on 05-June-2025
 */

@ExtendWith(MockitoExtension.class)
public class PaymentsNetworkVirtualCardApiAppTests {

	@Test
	public void testStaticMainMethod() {
		String[] args = new String[] { "args" };
		try (MockedStatic<SpringApplication> appConfig = Mockito.mockStatic(SpringApplication.class)) {
			SpringApplication.run(PaymentsNetworkVirtualCardApiApp.class, args);
			appConfig.verify(() -> PaymentsNetworkVirtualCardApiApp.main(args));
		}
	}

	@Test
	void eccEncryptionServiceBean_returnsECCEncryptionService() {
		PaymentsNetworkVirtualCardApiApp app = new PaymentsNetworkVirtualCardApiApp();
		AssymetricEncryptionService service = app.eccEncryptionService();
		Assertions.assertThat(service).isInstanceOf(ECCEncryptionService.class);
	}

}
