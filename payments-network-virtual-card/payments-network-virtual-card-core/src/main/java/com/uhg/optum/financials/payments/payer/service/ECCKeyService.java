package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.SetECCKeysDto;

public interface ECCKeyService {

	void setECCKeysIfNotExists(SetECCKeysDto setECCKeysDto);

	void rotateECCKeys();

}
