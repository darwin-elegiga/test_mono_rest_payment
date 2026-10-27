package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.CancelCardDto;

public interface CancelCardService {

	void sendCancelCardRequest(CancelCardDto cancelCardDto);

}
