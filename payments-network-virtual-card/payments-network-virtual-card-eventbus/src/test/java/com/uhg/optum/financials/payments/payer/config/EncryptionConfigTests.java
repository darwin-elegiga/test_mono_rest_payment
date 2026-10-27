package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.payments.payer.service.EncryptionAtRestService;
import com.uhg.optum.financials.payments.payer.service.impl.EncryptionAtRestServiceImpl;
import com.uhg.optum.financials.shared.core.encryption.AssymetricEncryptionService;
import com.uhg.optum.financials.shared.core.encryption.SymmetricEncryptionService;
import com.uhg.optum.financials.shared.core.encryption.impl.AESEncryptionService;
import com.uhg.optum.financials.shared.core.encryption.impl.ECCEncryptionService;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;

public class EncryptionConfigTests {

	private final EncryptionConfig encryptionConfig = new EncryptionConfig();

	@Test
	@DisplayName("should return ECCEncryptionService bean")
	void shouldReturnEccEncryptionServiceBean() {
		AssymetricEncryptionService service = this.encryptionConfig.eccEncryptionService();
		assertThat(service).isInstanceOf(ECCEncryptionService.class);
	}

	@Test
	@DisplayName("should return AESEncryptionService bean")
	void shouldReturnAesEncryptionServiceBean() {
		SymmetricEncryptionService service = this.encryptionConfig.aesEncryptionService();
		assertThat(service).isInstanceOf(AESEncryptionService.class);
	}

	@Test
	@DisplayName("should return EncryptionAtRestServiceImpl bean")
	void shouldReturnEncryptionAtRestServiceBean() {
		EncryptionAtRestService service = this.encryptionConfig.encryptionAtRestService();
		assertThat(service).isInstanceOf(EncryptionAtRestServiceImpl.class);
	}

}
