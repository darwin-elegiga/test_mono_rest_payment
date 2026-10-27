package com.uhg.optum.financials.payments.payer.wiremock;

/**
 * Main application class for standalone WireMock service.
 *
 * @author Payments Team
 */
public final class PaymentNetworkVirtualCardWireMockApp {

	private PaymentNetworkVirtualCardWireMockApp() {
	}

	public static void main(String[] args) {
		WireMockServerManager serverManager = WireMockServerManager.fromEnvironment();
		run(serverManager, (shutdownHook) -> Runtime.getRuntime().addShutdownHook(shutdownHook));
	}

	static void run(WireMockServerManager serverManager, ShutdownHookRegistrar shutdownHookRegistrar) {
		shutdownHookRegistrar.register(new Thread(serverManager::stop, "wiremock-shutdown-hook"));
		serverManager.start();
		serverManager.awaitUntilStopped();
	}

	@FunctionalInterface
	interface ShutdownHookRegistrar {

		void register(Thread shutdownHook);

	}

}
