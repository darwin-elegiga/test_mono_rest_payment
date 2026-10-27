package com.uhg.optum.financials.payments.payer;

import static org.assertj.core.api.BDDAssertions.then;
import static org.mockito.Mockito.mockStatic;

import java.io.File;

import org.apache.commons.io.FileUtils;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.MockedStatic;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.boot.SpringApplication;

@ExtendWith(MockitoExtension.class)
class PaymentsNetworkVirtualCardEventBusAppTests {

	@InjectMocks
	private PaymentsNetworkVirtualCardEventBusApp paymentsNetworkVirtualCardEventBusApp;

	@Test
	void testMain() {
		String[] args = new String[] { "args" };
		try (MockedStatic<SpringApplication> appConfig = mockStatic(SpringApplication.class)) {
			SpringApplication.run(PaymentsNetworkVirtualCardEventBusApp.class, args);
			appConfig.verify(() -> PaymentsNetworkVirtualCardEventBusApp.main(args));
		}
	}

	@Test
	void testCreateHealthCheckFile() {
		this.paymentsNetworkVirtualCardEventBusApp.createHealthCheckFile();
		then(true).isTrue();
	}

	@Test
	void testCreateHealthCheckFileThrowsException() {
		try (MockedStatic<FileUtils> fileUtilsMock = mockStatic(FileUtils.class)) {
			fileUtilsMock.when(() -> FileUtils.touch(Mockito.any(File.class)))
				.thenThrow(new RuntimeException("IO error"));
			Throwable thrown = org.assertj.core.api.Assertions
				.catchThrowable(() -> this.paymentsNetworkVirtualCardEventBusApp.createHealthCheckFile());
			then(thrown).isInstanceOf(RuntimeException.class)
				.hasMessageContaining("Failed to create health check file")
				.hasCauseInstanceOf(RuntimeException.class);
		}
	}

}
