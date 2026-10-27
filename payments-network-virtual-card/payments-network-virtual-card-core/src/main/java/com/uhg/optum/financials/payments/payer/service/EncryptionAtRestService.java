package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.shared.core.encryption.EncryptedData;

import java.io.IOException;
import java.security.GeneralSecurityException;

public interface EncryptionAtRestService {

	EncryptedData encrypt(String data);

	String encrypt(String data, byte[] keyBytes) throws GeneralSecurityException, IOException;

	String decrypt(String data);

	String decrypt(String data, byte[] keyInPemBytes) throws GeneralSecurityException, IOException;

	String decrypt(String data, String keyVersion) throws GeneralSecurityException, IOException;

}
