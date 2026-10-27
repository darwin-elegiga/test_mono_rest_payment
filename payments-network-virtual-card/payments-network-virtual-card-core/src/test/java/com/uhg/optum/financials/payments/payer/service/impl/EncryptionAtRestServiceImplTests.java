package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.shared.core.encryption.EncryptedData;
import com.uhg.optum.financials.shared.core.encryption.SymmetricEncryptionService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

import java.io.IOException;
import java.security.GeneralSecurityException;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.willThrow;

class EncryptionAtRestServiceImplTests {

	@Mock
	private SymmetricEncryptionService aesEncryptionService;

	@InjectMocks
	private EncryptionAtRestServiceImpl encryptionAtRestServiceImpl;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	void encrypt_shouldReturnEncryptedData_whenNoException() throws Exception {
		String data = "sensitive";
		EncryptedData encryptedData = new EncryptedData("cipherTextValue", "keyVersionValue");
		given(this.aesEncryptionService.encrypt(data, "symmetric-encryption-key-at-rest")).willReturn(encryptedData);
		EncryptedData result = this.encryptionAtRestServiceImpl.encrypt(data);
		assertThat(result).isEqualTo(encryptedData);
		assertThat(result.getCipherText()).isEqualTo("cipherTextValue");
		assertThat(result.getKeyVersion()).isEqualTo("keyVersionValue");
	}

	@Test
	void encrypt_shouldReturnNull_whenExceptionThrown() throws Exception {
		String data = "sensitive";
		willThrow(new GeneralSecurityException("fail")).given(this.aesEncryptionService)
			.encrypt(data, "symmetric-encryption-key-at-rest");
		assertThat(this.encryptionAtRestServiceImpl.encrypt(data)).isNull();
	}

	@Test
	void encrypt_withKeyBytes_shouldReturnString() throws Exception {
		String data = "sensitive";
		byte[] keyBytes = new byte[] { 1, 2, 3 };
		given(this.aesEncryptionService.encrypt(data, keyBytes)).willReturn("encrypted");
		assertThat(this.encryptionAtRestServiceImpl.encrypt(data, keyBytes)).isEqualTo("encrypted");
	}

	@Test
	void decrypt_shouldReturnString_whenNoException() throws Exception {
		String data = "encrypted";
		given(this.aesEncryptionService.decrypt(data, "symmetric-encryption-key-at-rest")).willReturn("decrypted");
		assertThat(this.encryptionAtRestServiceImpl.decrypt(data)).isEqualTo("decrypted");
	}

	@Test
	void decrypt_shouldReturnNull_whenExceptionThrown() throws Exception {
		String data = "encrypted";
		willThrow(new IOException("fail")).given(this.aesEncryptionService)
			.decrypt(data, "symmetric-encryption-key-at-rest");
		assertThat(this.encryptionAtRestServiceImpl.decrypt(data)).isNull();
	}

	@Test
	void decrypt_withKeyBytes_shouldReturnString() throws Exception {
		String data = "encrypted";
		byte[] keyBytes = new byte[] { 1, 2, 3 };
		given(this.aesEncryptionService.decrypt(data, keyBytes)).willReturn("decrypted");
		assertThat(this.encryptionAtRestServiceImpl.decrypt(data, keyBytes)).isEqualTo("decrypted");
	}

	@Test
	void decrypt_withKeyVersion_shouldReturnString() throws Exception {
		String data = "encrypted";
		String keyVersion = "v1";
		given(this.aesEncryptionService.decrypt(data, "symmetric-encryption-key-at-rest", keyVersion))
			.willReturn("decrypted");
		assertThat(this.encryptionAtRestServiceImpl.decrypt(data, keyVersion)).isEqualTo("decrypted");
	}

}
