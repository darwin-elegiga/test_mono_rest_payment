package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.SpendRulesDTO;
import com.uhg.optum.financials.payments.payer.dto.SpendRulesResponseDTO;
import com.uhg.optum.financials.payments.payer.model.SpendRules;

import java.util.List;

public interface SpendRulesConfigService {

	List<SpendRulesResponseDTO> updateSpendRuleConfigs(SpendRulesDTO spendRulesDTO, String orgShortCd);

	SpendRulesResponseDTO getSpendRuleConfigs(String orgShortCode, String accountShortCode, String subAccountShortCode);

	List<SpendRules> getAllSpendRuleConfigs();

	SpendRulesResponseDTO deleteSpendRuleConfigs(String orgShortCd, String accShortCd, String subAccShortCd);

	List<SpendRulesResponseDTO> loadSpendRuleConfigs(SpendRulesDTO spendRulesDTO, String orgShortCd);

}
