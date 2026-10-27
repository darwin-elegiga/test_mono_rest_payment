package com.uhg.optum.financials.payments.payer.wiremock;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.github.tomakehurst.wiremock.WireMockServer;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.lang.reflect.Method;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Locale;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Optional;
import java.util.concurrent.CountDownLatch;

import static com.github.tomakehurst.wiremock.core.WireMockConfiguration.options;

public class WireMockServerManager {

	private static final Logger log = LoggerFactory.getLogger(WireMockServerManager.class);

	private static final ObjectMapper OBJECT_MAPPER = new ObjectMapper();

	private static final int DEFAULT_PORT = 8080;

	private static final String DEFAULT_BIND_ADDRESS = "0.0.0.0";

	private final int port;

	private final String bindAddress;

	private final String configuredRootDir;

	private final boolean verbose;

	private final boolean logRequests;

	private WireMockServer wireMockServer;

	private volatile boolean running;

	private final CountDownLatch shutdownLatch = new CountDownLatch(1);

	public WireMockServerManager(int port, String bindAddress, String configuredRootDir, boolean verbose,
			boolean logRequests) {
		this.port = port;
		this.bindAddress = hasText(bindAddress) ? bindAddress.trim() : DEFAULT_BIND_ADDRESS;
		this.configuredRootDir = normalize(configuredRootDir);
		this.verbose = verbose;
		this.logRequests = logRequests;
	}

	public static WireMockServerManager fromEnvironment() {
		int port = readInt("wiremock.port", "WIREMOCK_PORT", DEFAULT_PORT);
		String bindAddress = readString("wiremock.bind-address", "WIREMOCK_BIND_ADDRESS", DEFAULT_BIND_ADDRESS);
		String rootDir = readString("wiremock.root-dir", "WIREMOCK_ROOT_DIR", "");
		boolean verbose = readBoolean("wiremock.verbose", "WIREMOCK_VERBOSE", true);
		boolean logRequests = readBoolean("wiremock.log-requests", "WIREMOCK_LOG_REQUESTS", true);
		return new WireMockServerManager(port, bindAddress, rootDir, verbose, logRequests);
	}

	public void start() {
		if (this.running) {
			return;
		}
		String rootDir = resolveRootDir();
		try {
			var config = options().port(this.port)
				.bindAddress(this.bindAddress)
				.usingFilesUnderDirectory(rootDir)
				.globalTemplating(true);

			if (isClassPresent("org.wiremock.webhooks.Webhooks")) {
				config.extensions("org.wiremock.webhooks.Webhooks");
			}

			if (this.verbose) {
				config.notifier(new com.github.tomakehurst.wiremock.common.Slf4jNotifier(true));
			}

			this.wireMockServer = new WireMockServer(config);
			registerRequestLogger(this.wireMockServer);
			this.wireMockServer.start();
			this.running = true;
			log.info("Standalone WireMock started on http://{}:{} with root-dir={}", this.bindAddress, this.port,
					rootDir);
		}
		catch (Exception ex) {
			log.error("Standalone WireMock failed to start on http://{}:{} with root-dir={}. cause={}",
					this.bindAddress, this.port, rootDir, ex.getMessage(), ex);
			throw ex;
		}
	}

	public void stop() {
		try {
			if (this.wireMockServer != null && this.wireMockServer.isRunning()) {
				this.wireMockServer.stop();
				log.info("Standalone WireMock stopped");
			}
		}
		finally {
			this.running = false;
			this.shutdownLatch.countDown();
		}
	}

	public boolean isRunning() {
		return this.running;
	}

	public void awaitUntilStopped() {
		try {
			this.shutdownLatch.await();
		}
		catch (InterruptedException ex) {
			Thread.currentThread().interrupt();
			log.warn("WireMock main thread interrupted, shutting down");
			stop();
		}
	}

	private String resolveRootDir() {
		String configured = normalize(this.configuredRootDir);
		if (hasText(configured) && Files.isDirectory(Path.of(configured))) {
			return configured;
		}

		Path cursor = Path.of(System.getProperty("user.dir"));
		while (cursor != null) {
			Path mappings = cursor.resolve("mappings");
			Path files = cursor.resolve("__files");
			if (Files.isDirectory(mappings) && Files.isDirectory(files)) {
				return cursor.toString();
			}
			cursor = cursor.getParent();
		}

		return System.getProperty("user.dir");
	}

	private static String normalize(String value) {
		return (value != null) ? value.trim() : null;
	}

	private static boolean hasText(String value) {
		return value != null && !value.trim().isEmpty();
	}

	private static String readString(String propertyName, String envName, String defaultValue) {
		String propertyValue = normalize(System.getProperty(propertyName));
		if (hasText(propertyValue)) {
			return propertyValue;
		}
		String envValue = normalize(System.getenv(envName));
		return hasText(envValue) ? envValue : defaultValue;
	}

	private static int readInt(String propertyName, String envName, int defaultValue) {
		String rawValue = readString(propertyName, envName, String.valueOf(defaultValue));
		try {
			int parsed = Integer.parseInt(rawValue);
			if (parsed < 1 || parsed > 65535) {
				throw new NumberFormatException("Port out of range: " + rawValue);
			}
			return parsed;
		}
		catch (NumberFormatException ex) {
			log.warn("Invalid integer value '{}' for {} / {}. Falling back to {}.", rawValue, propertyName, envName,
					defaultValue);
			return defaultValue;
		}
	}

	private static boolean readBoolean(String propertyName, String envName, boolean defaultValue) {
		String rawValue = readString(propertyName, envName, String.valueOf(defaultValue));
		if (!hasText(rawValue)) {
			return defaultValue;
		}
		switch (rawValue.trim().toLowerCase(Locale.ROOT)) {
			case "true", "1", "yes", "y", "on" -> {
				return true;
			}
			case "false", "0", "no", "n", "off" -> {
				return false;
			}
			default -> {
				log.warn("Invalid boolean value '{}' for {} / {}. Falling back to {}.", rawValue, propertyName, envName,
						defaultValue);
				return defaultValue;
			}
		}
	}

	private void registerRequestLogger(WireMockServer server) {
		if (!this.logRequests || server == null) {
			return;
		}
		server.addMockServiceRequestListener((request, response) -> {
			String method = Optional.ofNullable(request.getMethod()).map(Object::toString).orElse("UNKNOWN");
			String url = request.getUrl();
			int status = (response != null) ? response.getStatus() : -1;
			String requestBody = request.getBodyAsString();
			String responseBody = extractResponseBody(response);

			Map<String, Object> payload = new LinkedHashMap<>();
			payload.put("method", method);
			payload.put("url", url);
			payload.put("status", status);
			payload.put("requestBody", normalizeBodyForLog(requestBody));
			payload.put("responseBody", normalizeBodyForLog(responseBody));

			try {
				String prettyPayload = OBJECT_MAPPER.writerWithDefaultPrettyPrinter().writeValueAsString(payload);
				if (status >= 400) {
					log.error("WireMock handled error response:{}{}", System.lineSeparator(), prettyPayload);
				}
				else {
					log.info("WireMock handled request:{}{}", System.lineSeparator(), prettyPayload);
				}
			}
			catch (Exception ex) {
				if (status >= 400) {
					log.error("WireMock handled error response: method={} url={} status={}", method, url, status);
				}
				else {
					log.info("WireMock handled request: method={} url={} status={}", method, url, status);
				}
			}
		});
	}

	private Object normalizeBodyForLog(String body) {
		String truncatedBody = truncateBody(body);
		if (!hasText(truncatedBody)) {
			return "";
		}
		try {
			JsonNode jsonNode = OBJECT_MAPPER.readTree(truncatedBody);
			return jsonNode;
		}
		catch (Exception ex) {
			return truncatedBody;
		}
	}

	private String truncateBody(String body) {
		if (body == null) {
			return "";
		}
		String normalized = body.replaceAll("\\s+", " ").trim();
		int maxChars = 2000;
		return (normalized.length() > maxChars) ? normalized.substring(0, maxChars) + "..." : normalized;
	}

	private String extractResponseBody(Object response) {
		if (response == null) {
			return "";
		}
		String[] methodNames = { "getBodyAsString", "getBody" };
		for (String methodName : methodNames) {
			try {
				Method method = response.getClass().getMethod(methodName);
				Object value = method.invoke(response);
				if (value != null) {
					return String.valueOf(value);
				}
			}
			catch (Exception ignored) {
				// try next method
			}
		}
		return "";
	}

	private boolean isClassPresent(String className) {
		try {
			Class.forName(className);
			return true;
		}
		catch (ClassNotFoundException ex) {
			return false;
		}
	}

}
