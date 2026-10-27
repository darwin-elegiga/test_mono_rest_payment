package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.ConsentSaveResponseDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStatesSaveRequestDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStatesResponseDto;

public interface ConsentService {

	ConsentStatesResponseDto getAllStatesConsent();

	ConsentSaveResponseDto saveStatesConsent(ConsentStatesSaveRequestDto request);

}
