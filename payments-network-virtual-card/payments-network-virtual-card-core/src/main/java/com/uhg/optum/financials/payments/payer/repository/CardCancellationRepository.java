package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.CardCancellation;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.UUID;

@Repository
public interface CardCancellationRepository extends JpaRepository<CardCancellation, UUID> {

	CardCancellation findByPaymentId(String paymentId);

	CardCancellation findByCardGuid(UUID cardGuid);

}
