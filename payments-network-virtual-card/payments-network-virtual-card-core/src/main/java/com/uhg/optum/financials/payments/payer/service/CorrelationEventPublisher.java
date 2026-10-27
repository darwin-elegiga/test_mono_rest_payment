package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;

public interface CorrelationEventPublisher {

	/**
	 * publishes a correlation event to the worker queue for processing.
	 * @param correlationDto the correlation data containing batch and virtual card
	 * information
	 * @throws BadRequestException if publishing to the worker queue fails
	 */
	void publishCorrelationEvent(CorrelationDto correlationDto);

}
