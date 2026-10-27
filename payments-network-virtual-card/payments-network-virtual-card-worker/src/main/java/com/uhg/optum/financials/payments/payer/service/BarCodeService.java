package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.AcquirerAddress;

public interface BarCodeService {

	String generateBarCode(AcquirerAddress acquirerAddress);

}
