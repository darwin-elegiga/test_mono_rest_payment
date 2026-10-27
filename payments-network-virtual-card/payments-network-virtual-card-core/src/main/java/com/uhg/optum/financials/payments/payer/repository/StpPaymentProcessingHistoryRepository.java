package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.StpPaymentProcessingHistory;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface StpPaymentProcessingHistoryRepository extends JpaRepository<StpPaymentProcessingHistory, Long> {

}
