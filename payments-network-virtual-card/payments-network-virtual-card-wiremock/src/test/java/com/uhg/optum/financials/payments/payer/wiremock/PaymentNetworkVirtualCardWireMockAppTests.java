package com.uhg.optum.financials.payments.payer.wiremock;

import org.junit.jupiter.api.Test;
import org.mockito.InOrder;
import org.mockito.Mockito;

import java.util.concurrent.atomic.AtomicReference;

import static org.assertj.core.api.Assertions.assertThat;

class PaymentNetworkVirtualCardWireMockAppTests {

	@Test
	void runRegistersShutdownHookAndStartsManager() {
		WireMockServerManager manager = Mockito.mock(WireMockServerManager.class);
		AtomicReference<Thread> shutdownHookRef = new AtomicReference<>();

		PaymentNetworkVirtualCardWireMockApp.run(manager, shutdownHookRef::set);

		Thread shutdownHook = shutdownHookRef.get();
		assertThat(shutdownHook).isNotNull();
		assertThat(shutdownHook.getName()).isEqualTo("wiremock-shutdown-hook");

		InOrder inOrder = Mockito.inOrder(manager);
		inOrder.verify(manager).start();
		inOrder.verify(manager).awaitUntilStopped();
	}

	@Test
	void registeredShutdownHookInvokesStop() {
		WireMockServerManager manager = Mockito.mock(WireMockServerManager.class);
		AtomicReference<Thread> shutdownHookRef = new AtomicReference<>();

		PaymentNetworkVirtualCardWireMockApp.run(manager, shutdownHookRef::set);

		Thread shutdownHook = shutdownHookRef.get();
		assertThat(shutdownHook).isNotNull();
		shutdownHook.run();

		Mockito.verify(manager).stop();
	}

}
