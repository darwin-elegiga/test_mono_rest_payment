package com.uhg.optum.financials.payments.APITrigger;

import integration.service.TokenProvider;
import okhttp3.Interceptor;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.Response;
import retrofit2.Retrofit;
import retrofit2.converter.jackson.JacksonConverterFactory;

import java.io.IOException;
import java.time.Duration;
import java.util.Arrays;
import java.util.HashMap;

public final class RetrofitClient {

	private static final String MOCK_ZETA_CALLBACK_ENVIRONMENTS_PROPERTY = "mockZetaCallbackEnvironments";

	private static final String DEFAULT_MOCK_ZETA_CALLBACK_ENVIRONMENTS = "eph,dev";

	private RetrofitClient() {
	}

	public static HashMap<String, String> scenarioPaymentNumMap = new HashMap<>();

	public static HashMap<String, String> payerShortCodeMap = new HashMap<>();

	public static HashMap<String, String> orgShortCodeMap = new HashMap<>();

	public static HashMap<String, String> subPayerShortCodeMap = new HashMap<>();

	private static Retrofit retrofit = null;

	public static Retrofit getClient() {
		String token = TokenProvider.getToken();
		OkHttpClient okHttpClient = new OkHttpClient.Builder().connectTimeout(Duration.ofSeconds(60))
			.writeTimeout(Duration.ofSeconds(60))
			.readTimeout(Duration.ofSeconds(60))
			.addInterceptor(new Interceptor() {
				@Override
				public Response intercept(Chain chain) throws IOException {
					Request request = chain.request()
						.newBuilder()
						.addHeader("Authorization", "Bearer " + token)
						.build();
					return chain.proceed(request);
				}
			})
			.build();

		if (retrofit == null) {
			retrofit = new Retrofit.Builder().baseUrl(getBaseUrl())
				.client(okHttpClient)
				.addConverterFactory(JacksonConverterFactory.create())
				.build();
		}
		return retrofit;
	}

	private static String getBaseUrl() {
		return System.getProperty("baseURL");
	}

	public static String getEnv() {
		return System.getProperty("env");
	}

	public static boolean shouldMockZetaCallbacks() {
		return isCurrentEnvIn(
				System.getProperty(MOCK_ZETA_CALLBACK_ENVIRONMENTS_PROPERTY, DEFAULT_MOCK_ZETA_CALLBACK_ENVIRONMENTS));
	}

	private static boolean isCurrentEnvIn(String configuredEnvironments) {
		String env = getEnv();
		if (env == null || configuredEnvironments == null) {
			return false;
		}
		return Arrays.stream(configuredEnvironments.split(","))
			.map(String::trim)
			.filter(configuredEnvironment -> !configuredEnvironment.isEmpty())
			.anyMatch(configuredEnvironment -> configuredEnvironment.equalsIgnoreCase(env));
	}

}
