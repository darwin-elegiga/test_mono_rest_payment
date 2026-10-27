package com.uhg.optum.financials.payments.payer;

import com.uhg.optum.financials.shared.api.config.SecurityConfig;
import com.uhg.optum.financials.shared.api.config.SwaggerConfig;
import com.uhg.optum.financials.shared.core.config.KeyVaultConfig;
import com.uhg.optum.financials.shared.core.encryption.AssymetricEncryptionService;
import com.uhg.optum.financials.shared.core.encryption.impl.ECCEncryptionService;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientConfig;
import com.uhg.optum.financials.shared.worker.config.WorkerProducerConfig;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Import;
import org.springframework.retry.annotation.EnableRetry;
import org.springframework.scheduling.annotation.EnableScheduling;

/**
 * Main application class for PaymentsNetworkVirtualCardApp.
 *
 * @author alawaniy, created on 05-June-2025
 */
@SpringBootApplication(
		scanBasePackageClasses = { SecurityConfig.class, SwaggerConfig.class, KeyVaultConfig.class,
				WorkerProducerConfig.class },
		scanBasePackages = { "com.uhg.optum.financials.payments.payer.*", "com.uhg.optum.financials.shared.core.*" })
@EnableScheduling
@EnableRetry
@Import(ResilientRestClientConfig.class)
public class PaymentsNetworkVirtualCardApiApp {

	public static void main(String[] args) {
		SpringApplication.run(PaymentsNetworkVirtualCardApiApp.class, args);
	}

	@Bean
	public AssymetricEncryptionService eccEncryptionService() {
		return new ECCEncryptionService();
	}

}
