package com.uhg.optum.financials.payments.payer;

import com.uhg.optum.financials.payments.payer.config.JobNameProviderConfig;
import com.uhg.optum.financials.payments.payer.config.ZetaApiProperties;
import com.uhg.optum.financials.payments.payer.service.JobService;
import com.uhg.optum.financials.shared.core.config.KeyVaultConfig;
import com.uhg.optum.financials.shared.core.service.SFTPTransferService;
import com.uhg.optum.financials.shared.eventbus.kafka.config.KafkaBeans;
import com.uhg.optum.financials.shared.worker.config.WorkerProducerConfig;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.CommandLineRunner;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.WebApplicationType;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;
import tools.jackson.databind.ObjectMapper;

@SpringBootApplication(
		scanBasePackageClasses = { KeyVaultConfig.class, KafkaBeans.class, SFTPTransferService.class,
				WorkerProducerConfig.class },
		scanBasePackages = { "com.uhg.optum.financials.payments.payer.*",
				"com.uhg.optum.financials.shared.core.encryption.*", "com.uhg.optum.financials.shared.lib835.*",
				"com.uhg.optum.financials.shared.core.*" })
@EnableConfigurationProperties({ JobNameProviderConfig.class, ZetaApiProperties.class })
@EnableJpaRepositories
@Slf4j
public class PaymentLoadJobApp implements CommandLineRunner {

	private final JobService jobService;

	public PaymentLoadJobApp(JobService jobService) {
		this.jobService = jobService;
	}

	public static void main(String[] args) {
		SpringApplication application = new SpringApplication(PaymentLoadJobApp.class);
		application.setWebApplicationType(WebApplicationType.NONE);
		application.run(args);
	}

	@Bean
	public ObjectMapper objectMapper() {
		return new ObjectMapper();
	}

	@Override
	public void run(String... args) {
		this.jobService.checkAndSubmitJob();
	}

}
