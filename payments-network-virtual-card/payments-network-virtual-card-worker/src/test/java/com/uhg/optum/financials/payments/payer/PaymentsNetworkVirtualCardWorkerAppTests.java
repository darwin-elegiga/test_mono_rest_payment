package com.uhg.optum.financials.payments.payer;

import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockedStatic;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.boot.SpringApplication;

@ExtendWith(MockitoExtension.class)
class PaymentsNetworkVirtualCardWorkerAppTests {

	@InjectMocks
	private PaymentsNetworkVirtualCardWorkerApp paymentNetworkVirtualCardWorkerApp;

	@Mock
	private LogUtility logUtility;

	@Test
	void testMain() {
		String[] args = new String[] { "args" };
		try (MockedStatic<SpringApplication> appConfig = Mockito.mockStatic(SpringApplication.class)) {
			PaymentsNetworkVirtualCardWorkerApp.main(args);
			appConfig.verify(() -> SpringApplication.run(PaymentsNetworkVirtualCardWorkerApp.class, args));
		}
	}

	@Test
	void testCreateHealthCheckFile() {
		this.paymentNetworkVirtualCardWorkerApp.createHealthCheckFile();
		Assertions.assertThat(this.paymentNetworkVirtualCardWorkerApp).isNotNull();
	}

	@Test
	void testAesEncryptionService() {
		Assertions.assertThat(this.paymentNetworkVirtualCardWorkerApp.eccEncryptionService()).isNotNull();
	}

	@Test
	void testEccKeyService() {
		Assertions.assertThat(this.paymentNetworkVirtualCardWorkerApp.eccKeyService()).isNotNull();
	}

	@Test
	void testEccEncryptionService() {
		Assertions.assertThat(this.paymentNetworkVirtualCardWorkerApp.eccEncryptionService()).isNotNull();
	}

	@Test
	void testEncryptionAtRestService() {
		Assertions.assertThat(this.paymentNetworkVirtualCardWorkerApp.encryptionAtRestService()).isNotNull();
	}

	@Test
	void testSymmetricEncryptionService() {
		Assertions.assertThat(this.paymentNetworkVirtualCardWorkerApp.aesEncryptionService()).isNotNull();
	}

}
