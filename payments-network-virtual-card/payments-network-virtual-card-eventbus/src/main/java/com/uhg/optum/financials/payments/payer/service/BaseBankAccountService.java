package com.uhg.optum.financials.payments.payer.service;

import com.azure.identity.WorkloadIdentityCredentialBuilder;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.BaseEncryptedBankAccountDto;
import com.uhg.optum.financials.payments.payer.exception.BankAccountException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.core.config.KeyVaultConfig;
import com.uhg.optum.financials.shared.core.encryption.AssymetricEncryptionService;
import com.uhg.optum.financials.shared.core.http.BearerTokenInterceptor;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.lang3.StringUtils;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpHeaders;
import org.springframework.http.ResponseEntity;
import org.springframework.web.client.RestClient;
import org.springframework.web.util.UriComponentsBuilder;

import java.io.IOException;
import java.net.URI;
import java.security.GeneralSecurityException;
import java.util.Base64;
import java.util.UUID;

@Slf4j
public abstract class BaseBankAccountService {

	protected static final String ECC_PRIVATE_KEY_KEY = "ecc-encryption-private-key";

	protected static final String ECC_PUBLIC_KEY_KEY = "ecc-encryption-public-key";

	protected static final String PUBLIC_KEY_HEADER = "x-origin-public-key-base64";

	@Autowired
	protected LogUtility logUtility;

	@Autowired
	protected ResilientRestClientFactory resilientRestClientFactory;

	@Autowired
	protected AssymetricEncryptionService eccEncryptionService;

	@Autowired
	protected SecretService secretService;

	@Value("${azure.client.id:}")
	private String azureClientId;

	@Value("${azure.tenant.id:}")
	private String azureTenantId;

	@Value("${azure.federated.token.file:}")
	private String azureTokenFile;

	@Value("${spring.cloud.azure.active-directory.credential.client-id:}")
	private String azureAppClientId;

	@Value("${keyvault.kind:Local}")
	private String keyVaultKind;

	protected <T extends BaseEncryptedBankAccountDto> T getBankAccountById(String endpoint, UUID id,
			String orgShortCode, Class<T> responseType) {
		HttpHeaders headers = new HttpHeaders();

		if (orgShortCode != null) {
			headers.set("x-optum-tenant-id", orgShortCode);
		}

		String localPublicKey = this.secretService.getAssymetricEncryptionKeys(ECC_PUBLIC_KEY_KEY, null)
			.getPublicKeyValue();
		String localPublicKeyAsBase64 = Base64.getEncoder().encodeToString(localPublicKey.getBytes());

		headers.set(PUBLIC_KEY_HEADER, localPublicKeyAsBase64);

		URI uri = UriComponentsBuilder.fromUriString(endpoint).buildAndExpand(id).toUri();

		ResilientRestClient bankAccountClient = this.resilientRestClientFactory.getClient("bank-account",
				this::configureAuthentication);
		ResponseEntity<T> response = bankAccountClient.execute((client) -> client.get()
			.uri(uri)
			.headers((requestHeaders) -> requestHeaders.addAll(headers))
			.retrieve()
			.toEntity(responseType));

		T encryptedBankAccountDto = response.getBody();

		if (encryptedBankAccountDto == null) {
			log.error("Encrypted bank account not found for id: {}", id);
			throw new NotFoundException("Encrypted bank account not found for id: " + id);
		}

		try {
			String accountNumber = decryptFromOrigin(encryptedBankAccountDto.getPublicKeyAsBase64(),
					encryptedBankAccountDto.getEncryptedAccountNumber());
			encryptedBankAccountDto.setAccountNumber(accountNumber);
			return encryptedBankAccountDto;
		}
		catch (GeneralSecurityException | IOException ex) {
			log.error("Error while decrypting bank account number from remote for id: {} {}", id, ex.getMessage());
			throw new BankAccountException("Error occurred decrypting bank account number from remote ");
		}
	}

	private void configureAuthentication(RestClient.Builder builder) {
		if (StringUtils.equalsIgnoreCase(this.keyVaultKind, KeyVaultConfig.VaultKind.Azure.name())) {
			builder.requestInterceptor(
					new BearerTokenInterceptor(new WorkloadIdentityCredentialBuilder().clientId(this.azureClientId)
						.tenantId(this.azureTenantId)
						.tokenFilePath(this.azureTokenFile)
						.build(), this.azureAppClientId));
		}
	}

	private String decryptFromOrigin(String remotePublicKeyAsBase64, String cipherText)
			throws GeneralSecurityException, IOException {
		byte[] decodedBytes = Base64.getDecoder().decode(remotePublicKeyAsBase64);
		String remotePublicKeyInPem = new String(decodedBytes);
		String localPrivateKeyInPem = this.secretService.getAssymetricEncryptionKeys(null, ECC_PRIVATE_KEY_KEY)
			.getPrivateKeyValue();
		return this.eccEncryptionService.decrypt(cipherText, localPrivateKeyInPem, remotePublicKeyInPem);
	}

}
