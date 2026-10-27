package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.SpendRules;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface SpendRulesConfigRepository extends JpaRepository<SpendRules, Long> {

	Optional<SpendRules> findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode(String orgShortCd,
			String accShortCd, String subAccShortCd);

	Optional<SpendRules> findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCodeIsNull(String orgShortCd,
			String accShortCd);

	Optional<SpendRules> findByOrgShortCodeAndAccountShortCodeIsNullAndSubAccountShortCodeIsNull(String orgShortCd);

	List<SpendRules> findAll();

}
