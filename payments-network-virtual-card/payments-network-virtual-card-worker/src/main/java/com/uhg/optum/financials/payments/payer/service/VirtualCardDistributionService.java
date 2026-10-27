package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;

public interface VirtualCardDistributionService {

	void processCorrelationBatchEvent(CorrelationDto correlationDto);

}
