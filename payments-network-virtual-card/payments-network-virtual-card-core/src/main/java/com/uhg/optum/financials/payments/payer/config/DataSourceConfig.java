package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.shared.core.secrets.SecretService;
import com.zaxxer.hikari.HikariConfig;
import com.zaxxer.hikari.HikariDataSource;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.reactive.function.client.WebClient;

import javax.sql.DataSource;

// Disabled when Azure passwordless authentication is enabled (e.g., in ephemeral
// environments) so that Spring Boot's auto-configured DataSource is used instead.
@ConditionalOnProperty(name = "spring.datasource.azure.passwordless-enabled", havingValue = "false",
		matchIfMissing = true)
@Configuration
public class DataSourceConfig {

	private final SecretService secretService;

	@Value("${spring.datasource.url}")
	private String urlValue;

	@Value("${spring.datasource.driver-class-name}")
	private String driverClassName;

	@Value("${spring.datasource.hikari.maximum-pool-size}")
	private Integer maxPoolSize;

	@Value("${spring.datasource.hikari.minimum-idle}")
	private Integer minIdle;

	@Value("${spring.datasource.hikari.connection-timeout}")
	private Integer connectionTimeout;

	@Value("${spring.datasource.hikari.idle-timeout}")
	private Integer idleTimeout;

	@Value("${spring.datasource.hikari.leak-detection-threshold}")
	private Integer leakDetectionThreshold;

	@Value("${spring.datasource.hikari.max-lifetime}")
	private Integer maxLifetime;

	DataSourceConfig(SecretService secretService) {
		this.secretService = secretService;
	}

	@Bean
	public DataSource dataSource() {
		HikariConfig config = new HikariConfig();
		config.setDriverClassName(this.driverClassName);
		config.setJdbcUrl(this.urlValue);
		config.setUsername(this.secretService.getDatabaseCredentials().getUserName());
		config.setPassword(this.secretService.getDatabaseCredentials().getPassword());
		config.setMaximumPoolSize(this.maxPoolSize);
		config.setMinimumIdle(this.minIdle);
		config.setConnectionTimeout(this.connectionTimeout);
		config.setIdleTimeout(this.idleTimeout);
		config.setLeakDetectionThreshold(this.leakDetectionThreshold);
		config.setMaxLifetime(this.maxLifetime);
		return new HikariDataSource(config);
	}

	@Bean
	public WebClient webClient() {
		return WebClient.builder().build();
	}

}
