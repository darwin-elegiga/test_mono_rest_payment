package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface PaymentDetailsRepository extends JpaRepository<PaymentDetails, Long> {

	Optional<PaymentDetails> findById(String id);

	@Query(value = "SELECT nextval('virtual_card.network_payment_id_seq')", nativeQuery = true)
	Long getNextNetworkPaymentIdSequence();

}
