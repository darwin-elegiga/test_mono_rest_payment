package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.SetECCKeysDto;
import com.uhg.optum.financials.shared.core.encryption.AssymetricEncryptionService;
import com.uhg.optum.financials.shared.core.secrets.AssymetricSecretKeys;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import org.bouncycastle.crypto.AsymmetricCipherKeyPair;
import org.bouncycastle.crypto.params.AsymmetricKeyParameter;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.MockitoAnnotations;

import java.io.IOException;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.verify;

public class ECCKeyServiceImplTests {

	@Mock
	private SecretService secretService;

	@Mock
	private AssymetricEncryptionService eccEncryptionService;

	@InjectMocks
	private ECCKeyServiceImpl eccKeyService;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	void testSetECCKeysIfNotExists() throws IOException {
		// Test logic for setECCKeysIfNotExists
		AssymetricSecretKeys secretKeys = new AssymetricSecretKeys();

		Mockito.when(this.secretService.getAssymetricEncryptionKeys(anyString(), anyString())).thenReturn(secretKeys);

		AsymmetricCipherKeyPair keyPair = mock(AsymmetricCipherKeyPair.class);
		Mockito.when(this.eccEncryptionService.generateKeyPair()).thenReturn(keyPair);
		Mockito.when(keyPair.getPublic()).thenReturn(mock(AsymmetricKeyParameter.class));
		Mockito.when(this.eccEncryptionService.getPublicKeyInPemFormat(any())).thenReturn("publicKey");
		Mockito.when(this.eccEncryptionService.getPrivateKeyInPemFormat(any())).thenReturn("privateKey");
		SetECCKeysDto setECCKeysDto = new SetECCKeysDto();
		setECCKeysDto.setSetECCKeys(true);
		this.eccKeyService.setECCKeysIfNotExists(setECCKeysDto);

		verify(this.secretService).setAssymetricEncryptionKeys(any());
	}

	@Test
	void testSetECCKeysIfExists() {
		// Test logic for setECCKeysIfNotExists
		AssymetricSecretKeys secretKeys = new AssymetricSecretKeys();

		Mockito.when(this.secretService.getAssymetricEncryptionKeys(anyString(), anyString())).thenReturn(secretKeys);

		SetECCKeysDto setECCKeysDto = new SetECCKeysDto();
		setECCKeysDto.setSetECCKeys(true);
		this.eccKeyService.setECCKeysIfNotExists(setECCKeysDto);

		verify(this.secretService).getAssymetricEncryptionKeys(anyString(), anyString());

	}

}
