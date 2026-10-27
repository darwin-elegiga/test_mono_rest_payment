package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.service.EncryptionAtRestService;
import com.uhg.optum.financials.shared.core.encryption.EncryptedData;
import com.uhg.optum.financials.shared.core.encryption.SymmetricEncryptionService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.io.IOException;
import java.security.GeneralSecurityException;

@Slf4j
@Service
public class EncryptionAtRestServiceImpl implements EncryptionAtRestService {

	private static final String ENCRYPTION_AT_REST_KEY = "symmetric-encryption-key-at-rest";

	@Autowired
	private SymmetricEncryptionService aesEncryptionService;

	@Override
	public EncryptedData encrypt(String data) {
		try {
			return this.aesEncryptionService.encrypt(data, ENCRYPTION_AT_REST_KEY);
		}
		catch (GeneralSecurityException | IOException ex) {
			log.error("Error in encrypting data: {}", ex.getMessage());
		}
		return null;
	}

	public String encrypt(String data, byte[] keyBytes) throws GeneralSecurityException, IOException {

		return this.aesEncryptionService.encrypt(data, keyBytes);

	}

	@Override
	public String decrypt(String data) {
		try {
			return this.aesEncryptionService.decrypt(data, ENCRYPTION_AT_REST_KEY);
		}
		catch (GeneralSecurityException | IOException ex) {
			log.error("Error in decrypting data: {}", ex.getMessage());
		}
		return null;
	}

	public String decrypt(String data, byte[] keyInPemBytes) throws GeneralSecurityException, IOException {

		return this.aesEncryptionService.decrypt(data, keyInPemBytes);
	}

	@Override
	public String decrypt(String data, String keyVersion) throws GeneralSecurityException, IOException {
		return this.aesEncryptionService.decrypt(data, ENCRYPTION_AT_REST_KEY, keyVersion);

	}

}
