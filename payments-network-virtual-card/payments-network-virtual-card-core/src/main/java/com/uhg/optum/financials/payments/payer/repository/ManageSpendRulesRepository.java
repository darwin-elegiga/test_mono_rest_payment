package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.constant.RuleType;
import com.uhg.optum.financials.payments.payer.model.ManageSpendRules;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface ManageSpendRulesRepository extends JpaRepository<ManageSpendRules, Long> {

	ManageSpendRules findByRuleId(String ruleId);

	List<ManageSpendRules> findByRuleType(RuleType ruleType);

}
