package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.PaymentRequestHistory;
import org.springframework.data.jpa.repository.JpaRepository;

public interface PaymentRequestHistoryRepository extends JpaRepository<PaymentRequestHistory, Long> {

}
