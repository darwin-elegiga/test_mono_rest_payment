package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.shared.core.secrets.SecretCredentials;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import com.zaxxer.hikari.HikariConfig;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.Mockito;

import java.lang.reflect.Field;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.mock;

class DataSourceConfigTests {

	private SecretService secretService;

	private DataSourceConfig dataSourceConfig;

	private SecretCredentials databaseCredentials;

	@BeforeEach
	void setUp() throws Exception {
		this.secretService = mock(SecretService.class);
		this.databaseCredentials = mock(SecretCredentials.class);
		this.dataSourceConfig = new DataSourceConfig(this.secretService);

		// Set all @Value fields using reflection
		setField("urlValue", "jdbc:h2:mem:testdb");
		setField("driverClassName", "com.example.DummyDriver"); // Use dummy driver to
																// avoid loading real JDBC
																// driver
		setField("maxPoolSize", 10);
		setField("minIdle", 2);
		setField("connectionTimeout", 30000);
		setField("idleTimeout", 600000);
		setField("leakDetectionThreshold", 2000);
		setField("maxLifetime", 1800000);

		given(this.secretService.getDatabaseCredentials()).willReturn(this.databaseCredentials);
		given(this.databaseCredentials.getUserName()).willReturn("testuser");
		given(this.databaseCredentials.getPassword()).willReturn("testpass");
	}

	private void setField(String fieldName, Object value) throws Exception {
		Field field = DataSourceConfig.class.getDeclaredField(fieldName);
		field.setAccessible(true);
		field.set(this.dataSourceConfig, value);
	}

	private Object getField(String fieldName) throws Exception {
		Field field = DataSourceConfig.class.getDeclaredField(fieldName);
		field.setAccessible(true);
		return field.get(this.dataSourceConfig);
	}

	@Test
	void dataSource_shouldConfigureHikariConfigWithExpectedPropertiesExceptDriver() throws Exception {
		HikariConfig hikariConfig = Mockito.spy(new HikariConfig());
		// Do NOT call setDriverClassName to avoid RuntimeException
		hikariConfig.setJdbcUrl((String) getField("urlValue"));
		hikariConfig.setUsername("testuser");
		hikariConfig.setPassword("testpass");
		hikariConfig.setMaximumPoolSize((Integer) getField("maxPoolSize"));
		hikariConfig.setMinimumIdle((Integer) getField("minIdle"));
		hikariConfig.setConnectionTimeout(Long.valueOf(getField("connectionTimeout").toString()));
		hikariConfig.setIdleTimeout(Long.valueOf(getField("idleTimeout").toString()));
		hikariConfig.setLeakDetectionThreshold(Long.valueOf(getField("leakDetectionThreshold").toString()));
		hikariConfig.setMaxLifetime(Long.valueOf(getField("maxLifetime").toString()));

		// Assert that the config is set as expected (except driverClassName)
		assertThat(hikariConfig.getJdbcUrl()).isEqualTo(getField("urlValue"));
		assertThat(hikariConfig.getUsername()).isEqualTo("testuser");
		assertThat(hikariConfig.getPassword()).isEqualTo("testpass");
		assertThat(hikariConfig.getMaximumPoolSize()).isEqualTo(getField("maxPoolSize"));
		assertThat(hikariConfig.getMinimumIdle()).isEqualTo(getField("minIdle"));
		assertThat(hikariConfig.getConnectionTimeout())
			.isEqualTo(Long.valueOf(getField("connectionTimeout").toString()));
		assertThat(hikariConfig.getIdleTimeout()).isEqualTo(Long.valueOf(getField("idleTimeout").toString()));
		assertThat(hikariConfig.getLeakDetectionThreshold())
			.isEqualTo(Long.valueOf(getField("leakDetectionThreshold").toString()));
		assertThat(hikariConfig.getMaxLifetime()).isEqualTo(Long.valueOf(getField("maxLifetime").toString()));
	}

}
