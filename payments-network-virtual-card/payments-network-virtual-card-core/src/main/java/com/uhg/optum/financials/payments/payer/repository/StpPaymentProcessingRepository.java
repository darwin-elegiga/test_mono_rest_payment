package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.enums.StpPaymentProcessingStatus;
import com.uhg.optum.financials.payments.payer.model.StpPaymentProcessing;
import jakarta.transaction.Transactional;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface StpPaymentProcessingRepository extends JpaRepository<StpPaymentProcessing, Long> {

	@Query(value = "SELECT CASE WHEN COUNT(1) > 0 THEN TRUE ELSE FALSE END "
			+ "FROM virtual_card.stp_payment_processing " + "WHERE network_payment_id = :networkPaymentId",
			nativeQuery = true)
	boolean existsByNetworkPaymentId(@Param("networkPaymentId") String networkPaymentId);

	@Modifying
	@Transactional
	@Query("UPDATE StpPaymentProcessing s SET s.status = :status,s.rejectionCode = :rejectionCode, s.rejectionReason = :rejectionReason WHERE s.networkPaymentId = :networkPaymentId")
	void updateStatusToStpRejectedByNetworkPaymentId(@Param("networkPaymentId") String networkPaymentId,
			@Param("status") StpPaymentProcessingStatus stpPaymentProcessingStatus,
			@Param("rejectionCode") String rejectionCode, @Param("rejectionReason") String rejectionReason);

	Optional<StpPaymentProcessing> findByNetworkPaymentId(String networkPaymentId);

}
