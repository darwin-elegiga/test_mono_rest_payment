package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;

public interface VirtualCardResetService {

	void processResetCardRequest(ResetCardRequest resetCardRequest);

}
