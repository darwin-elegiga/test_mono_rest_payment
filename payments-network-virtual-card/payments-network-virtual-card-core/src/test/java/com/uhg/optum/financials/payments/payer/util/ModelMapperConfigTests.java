package com.uhg.optum.financials.payments.payer.util;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.modelmapper.ModelMapper;

import static org.assertj.core.api.Assertions.assertThat;

class ModelMapperConfigTests {

	@Test
	@DisplayName("modelMapper bean returns a ModelMapper instance")
	void modelMapper_shouldReturnModelMapperInstance() {
		// given
		ModelMapperConfig thisConfig = new ModelMapperConfig();
		// when
		ModelMapper thisMapper = thisConfig.modelMapper();
		// then
		assertThat(thisMapper).isNotNull();
		assertThat(thisMapper).isInstanceOf(ModelMapper.class);
	}

}
