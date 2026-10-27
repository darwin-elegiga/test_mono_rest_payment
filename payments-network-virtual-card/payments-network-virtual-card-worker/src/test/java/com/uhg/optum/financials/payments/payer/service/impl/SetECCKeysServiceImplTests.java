package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.SetECCKeysDto;
import com.uhg.optum.financials.payments.payer.service.ECCKeyService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.MockitoAnnotations;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.verify;

public class SetECCKeysServiceImplTests {

	@Mock
	private ECCKeyService eccKeyService;

	@InjectMocks
	private SetECCKeysServiceImpl setECCKeysService;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	void testSetKeysGenerates() {
		SetECCKeysDto setECCKeysDto = new SetECCKeysDto();
		setECCKeysDto.setSetECCKeys(true);

		Mockito.doNothing().when(this.eccKeyService).setECCKeysIfNotExists(any());

		this.setECCKeysService.setKeys(setECCKeysDto);

		verify(this.eccKeyService).setECCKeysIfNotExists(any());
	}

}
