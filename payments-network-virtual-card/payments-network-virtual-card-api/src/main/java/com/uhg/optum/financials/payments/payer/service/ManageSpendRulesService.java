package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.constant.RuleType;
import com.uhg.optum.financials.payments.payer.dto.RuleDto;
import com.uhg.optum.financials.payments.payer.model.ManageSpendRules;
import org.springframework.http.ResponseEntity;

public interface ManageSpendRulesService {

	String addSpendRule(RuleDto ruleDto);

	ManageSpendRules updateSpendRule(RuleDto ruleDto);

	ManageSpendRules getSpendRuleById(String ruleId);

	ResponseEntity<?> getSpendRulesByType(RuleType ruleType);

	ResponseEntity<?> getAllRules(Integer page, Integer size, String ruleType) throws Exception;

	ResponseEntity<?> getRuleBasedOnId(String ruleId) throws Exception;

	ResponseEntity<?> getRulesBasedOnTypeAndPagination(Integer page, Integer size, RuleType ruleType) throws Exception;

}
