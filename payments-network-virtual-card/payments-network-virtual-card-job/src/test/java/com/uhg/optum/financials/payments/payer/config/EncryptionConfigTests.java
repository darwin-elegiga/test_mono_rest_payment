package com.uhg.optum.financials.payments.payer.config;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
public class EncryptionConfigTests {

	@InjectMocks
	private EncryptionConfig encryptionConfig;

	@Test
	public void testAesEncryptionService() {
		this.encryptionConfig.aesEncryptionService();
	}

	@Test
	public void testEncryptionAtRestService() {
		this.encryptionConfig.encryptionAtRestService();
	}

	@Test
	public void testEccEncryptionService() {
		this.encryptionConfig.eccEncryptionService();
	}

}
