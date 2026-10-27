package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.model.CorrelationMapping;
import com.uhg.optum.financials.payments.payer.repository.CorrelationMappingRepository;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Optional;

import static org.mockito.BDDMockito.given;

@ExtendWith(MockitoExtension.class)
class OutboundCorrelationResolverImplTests {

	@Mock
	private CorrelationMappingRepository correlationMappingRepository;

	@InjectMocks
	private OutboundCorrelationResolverImpl outboundCorrelationResolver;

	@Test
	void resolveActualCorrelationId_returnsMappedValueWhenPresent() {
		CorrelationMapping mapping = new CorrelationMapping();
		mapping.setActualCorrelationId("123");
		mapping.setInternalCorrelationId("RRIABC");
		given(this.correlationMappingRepository.findById("RRIABC")).willReturn(Optional.of(mapping));

		Assertions.assertThat(this.outboundCorrelationResolver.resolveActualCorrelationId("RRIABC")).isEqualTo("123");
	}

	@Test
	void resolveActualCorrelationId_returnsInputWhenMappingMissing() {
		given(this.correlationMappingRepository.findById("456")).willReturn(Optional.empty());

		Assertions.assertThat(this.outboundCorrelationResolver.resolveActualCorrelationId("456")).isEqualTo("456");
	}

}
