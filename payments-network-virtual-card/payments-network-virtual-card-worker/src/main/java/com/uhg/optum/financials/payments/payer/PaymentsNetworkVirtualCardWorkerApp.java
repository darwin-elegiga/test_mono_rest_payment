package com.uhg.optum.financials.payments.payer;

import java.io.File;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.service.ECCKeyService;
import com.uhg.optum.financials.payments.payer.service.EncryptionAtRestService;
import com.uhg.optum.financials.payments.payer.service.impl.ECCKeyServiceImpl;
import com.uhg.optum.financials.payments.payer.service.impl.EncryptionAtRestServiceImpl;
import com.uhg.optum.financials.shared.core.config.KeyVaultConfig;
import com.uhg.optum.financials.shared.core.encryption.AssymetricEncryptionService;
import com.uhg.optum.financials.shared.core.encryption.SymmetricEncryptionService;
import com.uhg.optum.financials.shared.core.encryption.impl.AESEncryptionService;
import com.uhg.optum.financials.shared.core.encryption.impl.ECCEncryptionService;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientConfig;
import com.uhg.optum.financials.shared.core.service.SFTPTransferService;
import com.uhg.optum.financials.shared.eventbus.kafka.config.KafkaBeans;
import com.uhg.optum.financials.shared.worker.config.WorkerProducerConfig;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.io.FileUtils;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Import;
import org.springframework.context.event.EventListener;
import org.springframework.retry.annotation.EnableRetry;
import org.springframework.scheduling.annotation.EnableAsync;
import org.springframework.scheduling.annotation.EnableScheduling;

/**
 * Main application class for PaymentsNetworkVirtualCardWorkerApp.
 *
 * @author krambab1, created on 07-July-2025
 */

@Slf4j
@SpringBootApplication(
		scanBasePackageClasses = { KeyVaultConfig.class, KafkaBeans.class, SFTPTransferService.class,
				WorkerProducerConfig.class },
		scanBasePackages = { "com.uhg.optum.financials.payments.payer.*", "com.uhg.optum.financials.shared.lib835.*" })
@EnableScheduling
@EnableAsync
@EnableRetry
@EnableCaching
@Import(ResilientRestClientConfig.class)
public class PaymentsNetworkVirtualCardWorkerApp {

	public static final String FILE_DIR = "/tmp/health-check";

	public static void main(String[] args) {
		SpringApplication.run(PaymentsNetworkVirtualCardWorkerApp.class, args);
	}

	@Bean
	public AssymetricEncryptionService eccEncryptionService() {
		return new ECCEncryptionService();
	}

	@Bean
	public SymmetricEncryptionService aesEncryptionService() {
		return new AESEncryptionService();
	}

	@Bean
	public ECCKeyService eccKeyService() {
		return new ECCKeyServiceImpl();
	}

	@Bean
	public EncryptionAtRestService encryptionAtRestService() {
		return new EncryptionAtRestServiceImpl();
	}

	@Bean
	public ObjectMapper objectMapper() {
		return new ObjectMapper();
	}

	/**
	 * Creates a health check file in the specified directory. This method is triggered
	 * when the application is ready which is used for container helathcheck.
	 */
	@EventListener(ApplicationReadyEvent.class)
	public void createHealthCheckFile() {
		try {
			FileUtils.touch(new File(FILE_DIR));
		}
		catch (Exception ex) {
			log.error("Failed to create health check file at {}", FILE_DIR, ex);
		}
	}

}
