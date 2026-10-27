package com.uhg.optum.financials.payments.payer;

import com.uhg.optum.financials.shared.core.config.KeyVaultConfig;
import com.uhg.optum.financials.shared.core.config.LiquibaseConfig;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

/**
 * Main application class for PaymentsNetworkVirtualCardBootstrapApp.
 *
 * @author alawaniy, created on 05-June-2025
 */

@SpringBootApplication(scanBasePackageClasses = { KeyVaultConfig.class, LiquibaseConfig.class },
		scanBasePackages = "com.uhg.optum.financials.payments.payer.*")
@Slf4j
public class PaymentsNetworkVirtualCardBootstrapApp {

	public static void main(String[] args) {
		SpringApplication.run(PaymentsNetworkVirtualCardBootstrapApp.class, args);
	}

}
