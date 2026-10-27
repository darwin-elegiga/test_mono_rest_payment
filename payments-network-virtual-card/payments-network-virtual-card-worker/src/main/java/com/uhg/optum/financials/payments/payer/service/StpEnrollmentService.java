package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.stp.StpProviderEnrollmentRequestDto;

public interface StpEnrollmentService {

	void processEnrollmentRequest(StpProviderEnrollmentRequestDto request);

}
