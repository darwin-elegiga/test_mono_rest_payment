package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentCallbackDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpProviderEnrollmentRequestDto;

public interface StpEnrollmentOutboundClient {

	boolean sendValidationCallback(StpEnrollmentCallbackDto callbackDto, String endpoint);

	boolean sendToPreferencing(StpProviderEnrollmentRequestDto requestDto, String endpoint);

}
