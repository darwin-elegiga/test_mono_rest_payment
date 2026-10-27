package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.batch.BatchEventDto;

public interface BatchEventPublishService {

	String publishBatchEvent(BatchEventDto batchEventDto);

}
