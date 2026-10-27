package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.bankaccount.BaseEncryptedBankAccountDto;
import com.uhg.optum.financials.payments.payer.exception.BankAccountException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.shared.core.encryption.AssymetricEncryptionService;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.ResponseEntity;
import java.security.GeneralSecurityException;
import java.util.Base64;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.isNull;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.mock;

@ExtendWith(MockitoExtension.class)
class BaseBankAccountServiceTests {

	@Mock
	private ResilientRestClientFactory resilientRestClientFactory;

	@Mock
	private ResilientRestClient resilientRestClient;

	@Mock
	private AssymetricEncryptionService eccEncryptionService;

	@Mock
	private com.uhg.optum.financials.shared.core.secrets.SecretService secretService;

	private TestBankAccountService service;

	@BeforeEach
	void setUp() {
		this.service = new TestBankAccountService();
		this.service.resilientRestClientFactory = this.resilientRestClientFactory;
		this.service.eccEncryptionService = this.eccEncryptionService;
		this.service.secretService = this.secretService;
		given(this.resilientRestClientFactory.getClient(eq("bank-account"), any()))
			.willReturn(this.resilientRestClient);
	}

	@Test
	@DisplayName("should return decrypted bank account when found and decryption succeeds")
	void givenValidResponse_whenGetBankAccountById_thenReturnDecryptedDto() throws Exception {
		UUID id = UUID.randomUUID();
		String endpoint = "http://test/{id}";
		String orgShortCode = "ORG";
		String publicKey = "public-key";
		String publicKeyBase64 = Base64.getEncoder().encodeToString(publicKey.getBytes());
		String encryptedAccountNumber = "encrypted";
		String decryptedAccountNumber = "decrypted";
		String privateKey = "private-key";

		BaseEncryptedBankAccountDto dto = new BaseEncryptedBankAccountDto();
		dto.setPublicKeyAsBase64(publicKeyBase64);
		dto.setEncryptedAccountNumber(encryptedAccountNumber);

		// Mock AssymetricSecretKeys
		var publicKeyObj = mock(com.uhg.optum.financials.shared.core.secrets.AssymetricSecretKeys.class);
		var privateKeyObj = mock(com.uhg.optum.financials.shared.core.secrets.AssymetricSecretKeys.class);
		given(publicKeyObj.getPublicKeyValue()).willReturn(publicKey);
		given(privateKeyObj.getPrivateKeyValue()).willReturn(privateKey);
		given(this.secretService.getAssymetricEncryptionKeys(eq("ecc-encryption-public-key"), isNull()))
			.willReturn(publicKeyObj);
		given(this.secretService.getAssymetricEncryptionKeys(isNull(), eq("ecc-encryption-private-key")))
			.willReturn(privateKeyObj);
		given(this.resilientRestClient.execute(any())).willReturn(ResponseEntity.ok(dto));
		given(this.eccEncryptionService.decrypt(eq(encryptedAccountNumber), eq(privateKey), eq(publicKey)))
			.willReturn(decryptedAccountNumber);

		BaseEncryptedBankAccountDto result = this.service.getBankAccountById(endpoint, id, orgShortCode,
				BaseEncryptedBankAccountDto.class);
		assertThat(result.getAccountNumber()).isEqualTo(decryptedAccountNumber);
	}

	@Test
	@DisplayName("should throw NotFoundException when response body is null")
	void givenNullResponse_whenGetBankAccountById_thenThrowNotFound() {
		UUID id = UUID.randomUUID();
		String endpoint = "http://test/{id}";
		String orgShortCode = "ORG";
		var publicKeyObj = mock(com.uhg.optum.financials.shared.core.secrets.AssymetricSecretKeys.class);
		given(publicKeyObj.getPublicKeyValue()).willReturn("public-key");
		given(this.secretService.getAssymetricEncryptionKeys(eq("ecc-encryption-public-key"), isNull()))
			.willReturn(publicKeyObj);
		given(this.resilientRestClient.execute(any())).willReturn(ResponseEntity.ok(null));

		assertThatThrownBy(
				() -> this.service.getBankAccountById(endpoint, id, orgShortCode, BaseEncryptedBankAccountDto.class))
			.isInstanceOf(NotFoundException.class)
			.hasMessageContaining("Encrypted bank account not found for id");
	}

	@Test
	@DisplayName("should throw BankAccountException when decryption fails")
	void givenDecryptionFails_whenGetBankAccountById_thenThrowBankAccountException() throws Exception {
		UUID id = UUID.randomUUID();
		String endpoint = "http://test/{id}";
		String orgShortCode = "ORG";
		String publicKey = "public-key";
		String publicKeyBase64 = Base64.getEncoder().encodeToString(publicKey.getBytes());
		String encryptedAccountNumber = "encrypted";
		String privateKey = "private-key";

		BaseEncryptedBankAccountDto dto = new BaseEncryptedBankAccountDto();
		dto.setPublicKeyAsBase64(publicKeyBase64);
		dto.setEncryptedAccountNumber(encryptedAccountNumber);

		var publicKeyObj = mock(com.uhg.optum.financials.shared.core.secrets.AssymetricSecretKeys.class);
		var privateKeyObj = mock(com.uhg.optum.financials.shared.core.secrets.AssymetricSecretKeys.class);
		given(publicKeyObj.getPublicKeyValue()).willReturn(publicKey);
		given(privateKeyObj.getPrivateKeyValue()).willReturn(privateKey);
		given(this.secretService.getAssymetricEncryptionKeys(eq("ecc-encryption-public-key"), isNull()))
			.willReturn(publicKeyObj);
		given(this.secretService.getAssymetricEncryptionKeys(isNull(), eq("ecc-encryption-private-key")))
			.willReturn(privateKeyObj);
		given(this.resilientRestClient.execute(any())).willReturn(ResponseEntity.ok(dto));
		given(this.eccEncryptionService.decrypt(anyString(), anyString(), anyString()))
			.willThrow(new GeneralSecurityException("fail"));

		assertThatThrownBy(
				() -> this.service.getBankAccountById(endpoint, id, orgShortCode, BaseEncryptedBankAccountDto.class))
			.isInstanceOf(BankAccountException.class)
			.hasMessageContaining("Error occurred decrypting bank account number from remote");
	}

	// Helper concrete class for testing
	static class TestBankAccountService extends BaseBankAccountService {

	}

}
