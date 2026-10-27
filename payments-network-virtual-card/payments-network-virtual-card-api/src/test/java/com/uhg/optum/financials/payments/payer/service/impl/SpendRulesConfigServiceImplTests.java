package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.SpendRules;
import com.uhg.optum.financials.payments.payer.repository.SpendRulesConfigRepository;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Arrays;
import java.util.Collections;
import java.util.List;

@ExtendWith(MockitoExtension.class)
public class SpendRulesConfigServiceImplTests {

	@InjectMocks
	private SpendRulesConfigServiceImpl spendRulesConfigService;

	@Mock
	private SpendRulesConfigRepository spendRulesConfigRepository;

	@Test
	void testGetAllSpendRuleConfigs_ReturnsList() {
		SpendRules rule1 = new SpendRules();
		SpendRules rule2 = new SpendRules();
		List<SpendRules> mockList = Arrays.asList(rule1, rule2);

		BDDMockito.when(this.spendRulesConfigRepository.findAll()).thenReturn(mockList);

		List<SpendRules> result = this.spendRulesConfigService.getAllSpendRuleConfigs();

		Assertions.assertThat(result).isNotNull();
	}

	@Test
	void testGetAllSpendRuleConfigs_ReturnsEmptyList() {
		BDDMockito.when(this.spendRulesConfigRepository.findAll()).thenReturn(Collections.emptyList());

		Assertions.assertThatThrownBy(() -> this.spendRulesConfigService.getAllSpendRuleConfigs())
			.isInstanceOf(NotFoundException.class)
			.hasMessage("No spend rules configurations found.");
	}

}
