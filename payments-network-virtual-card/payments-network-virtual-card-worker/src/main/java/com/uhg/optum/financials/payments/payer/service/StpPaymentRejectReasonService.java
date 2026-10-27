package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.stp.StpRejectReasonDto;

public interface StpPaymentRejectReasonService {

	void processRejectReason(StpRejectReasonDto dto);

}
