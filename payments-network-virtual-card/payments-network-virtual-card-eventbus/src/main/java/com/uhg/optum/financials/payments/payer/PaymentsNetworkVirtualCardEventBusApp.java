package com.uhg.optum.financials.payments.payer;

import com.uhg.optum.financials.payments.payer.config.EventBusProperties;
import com.uhg.optum.financials.shared.core.config.KeyVaultConfig;
import com.uhg.optum.financials.shared.core.config.LiquibaseConfig;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientConfig;
import com.uhg.optum.financials.shared.eventbus.kafka.config.KafkaBeans;
import com.uhg.optum.financials.shared.eventbus.kafka.config.KafkaConsumerConfig;
import com.uhg.optum.financials.shared.worker.config.WorkerProducerConfig;
import org.apache.commons.io.FileUtils;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.context.event.EventListener;
import org.springframework.context.annotation.Import;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;
import org.springframework.retry.annotation.EnableRetry;
import org.springframework.validation.Validator;

import java.io.File;

@SpringBootApplication(
		scanBasePackageClasses = { Validator.class, KeyVaultConfig.class, KafkaBeans.class, KafkaConsumerConfig.class,
				LiquibaseConfig.class, WorkerProducerConfig.class },
		scanBasePackages = "com.uhg.optum.financials.payments.payer.*")
@EnableConfigurationProperties(EventBusProperties.class)
@EnableJpaRepositories
@EnableRetry
@Import(ResilientRestClientConfig.class)
public class PaymentsNetworkVirtualCardEventBusApp {

	public static void main(String[] args) {
		SpringApplication.run(PaymentsNetworkVirtualCardEventBusApp.class, args);
	}

	@EventListener(ApplicationReadyEvent.class)
	public void createHealthCheckFile() {
		try {
			FileUtils.touch(new File(System.getProperty("java.io.tmpdir"), "health-check"));
		}
		catch (Exception ex) {
			throw new RuntimeException("Failed to create health check file", ex);
		}
	}

}
