package com.uhg.optum.financials.payments.payer.wiremock;

import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

import java.io.IOException;
import java.lang.reflect.Field;
import java.net.ServerSocket;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.Duration;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.TimeUnit;

import static org.assertj.core.api.Assertions.assertThat;

class WireMockServerManagerTests {

	private static final String PROP_PORT = "wiremock.port";

	private static final String PROP_BIND_ADDRESS = "wiremock.bind-address";

	private static final String PROP_ROOT_DIR = "wiremock.root-dir";

	private static final String PROP_VERBOSE = "wiremock.verbose";

	private static final String PROP_LOG_REQUESTS = "wiremock.log-requests";

	@AfterEach
	void clearProperties() {
		System.clearProperty(PROP_PORT);
		System.clearProperty(PROP_BIND_ADDRESS);
		System.clearProperty(PROP_ROOT_DIR);
		System.clearProperty(PROP_VERBOSE);
		System.clearProperty(PROP_LOG_REQUESTS);
	}

	@Test
	void fromEnvironmentUsesDefaultsWhenPropertiesMissing() throws Exception {
		WireMockServerManager manager = WireMockServerManager.fromEnvironment();

		assertThat(getIntField(manager, "port")).isEqualTo(8080);
		assertThat(getStringField(manager, "bindAddress")).isEqualTo("0.0.0.0");
		assertThat(getStringField(manager, "configuredRootDir")).isEqualTo("");
		assertThat(getBooleanField(manager, "verbose")).isTrue();
		assertThat(getBooleanField(manager, "logRequests")).isTrue();
	}

	@Test
	void fromEnvironmentParsesPropertiesAndFallsBackForInvalidPort() throws Exception {
		System.setProperty(PROP_PORT, "70000");
		System.setProperty(PROP_BIND_ADDRESS, " 127.0.0.1 ");
		System.setProperty(PROP_ROOT_DIR, " custom-root ");
		System.setProperty(PROP_VERBOSE, "false");
		System.setProperty(PROP_LOG_REQUESTS, "off");

		WireMockServerManager manager = WireMockServerManager.fromEnvironment();

		assertThat(getIntField(manager, "port")).isEqualTo(8080);
		assertThat(getStringField(manager, "bindAddress")).isEqualTo("127.0.0.1");
		assertThat(getStringField(manager, "configuredRootDir")).isEqualTo("custom-root");
		assertThat(getBooleanField(manager, "verbose")).isFalse();
		assertThat(getBooleanField(manager, "logRequests")).isFalse();
	}

	@Test
	void startAndStopUpdateRunningState() throws Exception {
		Path rootDir = createWireMockRootDir();
		int port = findAvailablePort();
		WireMockServerManager manager = new WireMockServerManager(port, "127.0.0.1", rootDir.toString(), false, false);

		assertThat(manager.isRunning()).isFalse();
		manager.start();
		assertThat(manager.isRunning()).isTrue();
		manager.stop();
		assertThat(manager.isRunning()).isFalse();
	}

	@Test
	void awaitUntilStoppedUnblocksAfterStop() throws Exception {
		WireMockServerManager manager = new WireMockServerManager(8080, "127.0.0.1", "", false, false);

		CompletableFuture<Void> waiter = CompletableFuture.runAsync(manager::awaitUntilStopped);
		TimeUnit.MILLISECONDS.sleep(100);
		manager.stop();

		waiter.orTimeout(Duration.ofSeconds(2).toMillis(), TimeUnit.MILLISECONDS).join();
		assertThat(waiter.isDone()).isTrue();
	}

	private static Path createWireMockRootDir() throws IOException {
		Path rootDir = Files.createTempDirectory("wiremock-test-root");
		Files.createDirectories(rootDir.resolve("mappings"));
		Files.createDirectories(rootDir.resolve("__files"));
		return rootDir;
	}

	private static int findAvailablePort() throws IOException {
		try (ServerSocket socket = new ServerSocket(0)) {
			return socket.getLocalPort();
		}
	}

	private static int getIntField(Object target, String fieldName) throws Exception {
		return (int) getField(target, fieldName).get(target);
	}

	private static String getStringField(Object target, String fieldName) throws Exception {
		return (String) getField(target, fieldName).get(target);
	}

	private static boolean getBooleanField(Object target, String fieldName) throws Exception {
		return (boolean) getField(target, fieldName).get(target);
	}

	private static Field getField(Object target, String fieldName) throws Exception {
		Field field = target.getClass().getDeclaredField(fieldName);
		field.setAccessible(true);
		return field;
	}

}
