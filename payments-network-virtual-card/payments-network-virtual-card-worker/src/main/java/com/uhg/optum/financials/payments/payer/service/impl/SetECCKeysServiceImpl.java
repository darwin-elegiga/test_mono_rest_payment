package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.SetECCKeysDto;
import com.uhg.optum.financials.payments.payer.service.ECCKeyService;
import com.uhg.optum.financials.payments.payer.service.SetECCKeysService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.util.Assert;

@Slf4j
@Service
public class SetECCKeysServiceImpl implements SetECCKeysService {

	private final ECCKeyService eccKeyService;

	public SetECCKeysServiceImpl(ECCKeyService eccKeyService) {
		this.eccKeyService = eccKeyService;
	}

	@Override
	public void setKeys(SetECCKeysDto setECCKeysDto) {
		Assert.notNull(setECCKeysDto, "event must not be null");

		this.eccKeyService.setECCKeysIfNotExists(setECCKeysDto);
	}

}
