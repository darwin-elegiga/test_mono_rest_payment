package com.uhg.optum.financials.payments.payer;

import com.uhg.optum.financials.shared.api.config.SwaggerConfig;
import com.uhg.optum.financials.shared.core.config.KeyVaultConfig;
import com.uhg.optum.financials.shared.core.config.LiquibaseConfig;
import com.uhg.optum.financials.shared.core.encryption.AssymetricEncryptionService;
import com.uhg.optum.financials.shared.core.encryption.impl.ECCEncryptionService;
import com.uhg.optum.financials.shared.core.service.SFTPTransferService;
import com.uhg.optum.financials.shared.eventbus.kafka.config.KafkaBeans;
import com.uhg.optum.financials.shared.eventbus.kafka.config.KafkaConsumerConfig;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.validation.Validator;

/**
 * Main application class for PaymentsNetworkVirtualCardMockApp.
 *
 * @author alawaniy, created on 05-June-2025
 */

@SpringBootApplication(
		scanBasePackageClasses = { Validator.class, KeyVaultConfig.class, KafkaBeans.class, KafkaConsumerConfig.class,
				SwaggerConfig.class, LiquibaseConfig.class, SFTPTransferService.class },
		scanBasePackages = "com.uhg.optum.financials.payments.payer.*")
public class PaymentsNetworkVirtualCardMockApp {

	@Bean
	public AssymetricEncryptionService eccEncryptionService() {
		return new ECCEncryptionService();
	}

	public static void main(String[] args) {
		SpringApplication.run(PaymentsNetworkVirtualCardMockApp.class, args);
	}

}
