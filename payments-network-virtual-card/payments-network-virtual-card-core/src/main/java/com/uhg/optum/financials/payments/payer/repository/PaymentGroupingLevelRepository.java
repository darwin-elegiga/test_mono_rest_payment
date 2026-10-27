package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.PaymentGroupingLevel;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface PaymentGroupingLevelRepository extends JpaRepository<PaymentGroupingLevel, Long> {

	Optional<PaymentGroupingLevel> findByOrgShortCode(String orgShortCode);

}
