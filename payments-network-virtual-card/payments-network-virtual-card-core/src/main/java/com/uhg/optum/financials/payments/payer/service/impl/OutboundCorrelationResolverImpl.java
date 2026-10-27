package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.repository.CorrelationMappingRepository;
import com.uhg.optum.financials.payments.payer.service.OutboundCorrelationResolver;
import org.springframework.stereotype.Service;

@Service
public class OutboundCorrelationResolverImpl implements OutboundCorrelationResolver {

	private final CorrelationMappingRepository correlationMappingRepository;

	public OutboundCorrelationResolverImpl(CorrelationMappingRepository correlationMappingRepository) {
		this.correlationMappingRepository = correlationMappingRepository;
	}

	@Override
	public String resolveActualCorrelationId(String processingCorrelationId) {
		return this.correlationMappingRepository.findById(processingCorrelationId)
			.map((mapping) -> mapping.getActualCorrelationId())
			.orElse(processingCorrelationId);
	}

}
