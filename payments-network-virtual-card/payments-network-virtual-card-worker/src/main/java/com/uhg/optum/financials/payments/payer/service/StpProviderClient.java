package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.stp.StpVirtualCardRequestDto;

public interface StpProviderClient {

	void postRemittance(String providerKey, ProviderConfig providerConfig, StpVirtualCardRequestDto payload,
			String networkPaymentId, Long paymentRequestId);

	record ProviderConfig(String endpoint, String authType) {
	}

}
