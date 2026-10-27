package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.FileTracking;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.transaction.annotation.Propagation;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.Optional;

public interface FileTrackingRepository extends JpaRepository<FileTracking, String> {

	Optional<FileTracking> findByCorrelationId(String correlationId);

	@Modifying
	@Transactional
	void deleteByCorrelationId(String correlationId);

	@Query("SELECT ft.correlationId FROM FileTracking ft WHERE ft.correlationId IN :correlationIds")
	List<String> findExistingCorrelationIds(@Param("correlationIds") List<String> correlationIds);

	@Query("SELECT :fileType FROM FileTracking ft WHERE ft.correlationId = :correlationId")
	String findFileName(@Param("fileType") String fileType, @Param("correlationId") String correlationId);

	@Query("SELECT ft FROM FileTracking ft WHERE ft.correlationId = :correlationId")
	FileTracking findFileNameByCorrelationId(@Param("correlationId") String correlationId);

	@Query("""
			UPDATE FileTracking ft
			SET ft.companion = :companionPath, ft.companionStatus = :status
			WHERE ft.correlationId = :correlationId
			""")
	@Modifying(clearAutomatically = true, flushAutomatically = true)
	@Transactional(propagation = Propagation.REQUIRES_NEW)
	int updateCompanionAndStatusByCorrelationId(@Param("correlationId") String correlationId,
			@Param("companionPath") String companionPath, @Param("status") String status);

	@Query("UPDATE FileTracking ft SET ft.companionStatus = :status WHERE ft.correlationId = :correlationId")
	@Modifying(clearAutomatically = true, flushAutomatically = true)
	@Transactional(propagation = Propagation.REQUIRES_NEW)
	void updateCompanionStatusByCorrelationId(@Param("correlationId") String correlationId,
			@Param("status") String status);

	@Query("UPDATE FileTracking ft SET ft.paymentActivityStatus = :status WHERE ft.correlationId = :correlationId")
	@Modifying(clearAutomatically = true, flushAutomatically = true)
	@Transactional(propagation = Propagation.REQUIRES_NEW)
	void updatePaymentActivityStatusByCorrelationId(@Param("correlationId") String correlationId,
			@Param("status") String status);

}
