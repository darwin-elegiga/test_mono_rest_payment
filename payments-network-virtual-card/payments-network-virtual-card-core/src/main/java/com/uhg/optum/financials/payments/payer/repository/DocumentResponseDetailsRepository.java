package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.DocumentResponseDetails;
import jakarta.transaction.Transactional;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Set;

@Repository
public interface DocumentResponseDetailsRepository extends JpaRepository<DocumentResponseDetails, Long> {

	@Query(value = "SELECT * FROM virtual_card.document_response_details WHERE network_payment_id = :networkPaymentId",
			nativeQuery = true)
	List<DocumentResponseDetails> findByNetworkPaymentId(@Param("networkPaymentId") String networkPaymentId);

	@Transactional
	@Modifying
	void deleteAllByNetworkPaymentIdIn(Set<String> networkPaymentId);

}
