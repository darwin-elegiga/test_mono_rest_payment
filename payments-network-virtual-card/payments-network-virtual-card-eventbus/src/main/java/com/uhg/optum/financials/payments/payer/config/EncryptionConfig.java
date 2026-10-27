package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.payments.payer.service.EncryptionAtRestService;
import com.uhg.optum.financials.payments.payer.service.impl.EncryptionAtRestServiceImpl;
import com.uhg.optum.financials.shared.core.encryption.AssymetricEncryptionService;
import com.uhg.optum.financials.shared.core.encryption.SymmetricEncryptionService;
import com.uhg.optum.financials.shared.core.encryption.impl.AESEncryptionService;
import com.uhg.optum.financials.shared.core.encryption.impl.ECCEncryptionService;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class EncryptionConfig {

	@Bean
	public AssymetricEncryptionService eccEncryptionService() {
		return new ECCEncryptionService();
	}

	@Bean
	public SymmetricEncryptionService aesEncryptionService() {
		return new AESEncryptionService();
	}

	@Bean
	public EncryptionAtRestService encryptionAtRestService() {
		return new EncryptionAtRestServiceImpl();
	}

}
