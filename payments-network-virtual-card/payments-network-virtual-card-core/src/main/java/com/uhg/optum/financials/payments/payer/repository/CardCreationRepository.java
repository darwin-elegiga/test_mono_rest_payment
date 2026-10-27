package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.CardCreation;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;
import java.util.UUID;

@Repository
public interface CardCreationRepository extends JpaRepository<CardCreation, UUID> {

	/**
	 * Returns the most recent card-creation attempt for a payment. A payment may have
	 * more than one {@link CardCreation} row across retries, so this — not an
	 * assumed-unique lookup by payment id — is the one to use anywhere that needs the
	 * current/authoritative state of a payment's card creation.
	 */
	Optional<CardCreation> findTopByPaymentIdOrderByCreatedAtDesc(String paymentId);

}
