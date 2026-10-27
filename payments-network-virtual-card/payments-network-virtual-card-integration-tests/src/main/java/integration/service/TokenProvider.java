package integration.service;

import com.uhg.optum.financials.shared.core.secrets.services.TokenService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.io.InputStream;
import java.util.Properties;

public final class TokenProvider {

	private static final Logger log = LoggerFactory.getLogger(TokenProvider.class);

	private TokenProvider() {
	}

	public static String getToken() {
		String clientId = readingPropertyFileForClientId();
		return TokenService.getAccessToken(clientId);
	}

	private static String readingPropertyFileForClientId() {
		String appConfigPath = "application.properties";
		ClassLoader loader = Thread.currentThread().getContextClassLoader();
		Properties appProps = new Properties();
		try (InputStream fileInputStream = loader.getResourceAsStream(appConfigPath)) {
			appProps.load(fileInputStream);
		}
		catch (IOException ex) {
			log.error("Error reading application.properties file", ex);
		}
		return appProps.getProperty("clientId");
	}

}
