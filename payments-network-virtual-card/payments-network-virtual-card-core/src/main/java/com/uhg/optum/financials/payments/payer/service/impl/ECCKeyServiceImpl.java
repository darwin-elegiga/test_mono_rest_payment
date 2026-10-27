package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.SetECCKeysDto;
import com.uhg.optum.financials.payments.payer.service.ECCKeyService;
import com.uhg.optum.financials.shared.core.encryption.AssymetricEncryptionService;
import com.uhg.optum.financials.shared.core.secrets.AssymetricSecretKeys;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.lang3.StringUtils;
import org.bouncycastle.crypto.AsymmetricCipherKeyPair;
import org.springframework.beans.factory.annotation.Autowired;

@Slf4j
public class ECCKeyServiceImpl implements ECCKeyService {

	private static final String ECC_PUBLIC_KEY_KEY = "ecc-encryption-public-key";

	private static final String ECC_PRIVATE_KEY_KEY = "ecc-encryption-private-key";

	@Autowired
	private AssymetricEncryptionService eccEncryptionService;

	@Autowired
	private SecretService secretService;

	@Override
	public void setECCKeysIfNotExists(SetECCKeysDto setECCKeysDto) {
		AssymetricSecretKeys secretKeys = this.secretService.getAssymetricEncryptionKeys(ECC_PUBLIC_KEY_KEY,
				ECC_PRIVATE_KEY_KEY);

		if (!StringUtils.isEmpty(secretKeys.getPublicKeyValue()) || !setECCKeysDto.isSetECCKeys()) {
			log.info("ECC keys already exists, skipping the key generation");
			return;
		}

		rotateECCKeys();
	}

	@Override
	public void rotateECCKeys() {
		log.info("Rotating the ECC encryption key pair");
		log.info("Generating a new ECC key pair");
		try {
			AsymmetricCipherKeyPair keys = this.eccEncryptionService.generateKeyPair();

			String publicKey = this.eccEncryptionService.getPublicKeyInPemFormat(keys.getPublic());
			String privateKey = this.eccEncryptionService.getPrivateKeyInPemFormat(keys.getPrivate());
			AssymetricSecretKeys secretKeys = new AssymetricSecretKeys();
			secretKeys.setPublicKeyKey(ECC_PUBLIC_KEY_KEY);
			secretKeys.setPublicKeyValue(publicKey);
			secretKeys.setPrivateKeyKey(ECC_PRIVATE_KEY_KEY);
			secretKeys.setPrivateKeyValue(privateKey);
			this.secretService.setAssymetricEncryptionKeys(secretKeys);
		}
		catch (Exception ex) {
			log.error("Error while rotating ECC keys: {}", ex.getMessage());

		}
	}

}
