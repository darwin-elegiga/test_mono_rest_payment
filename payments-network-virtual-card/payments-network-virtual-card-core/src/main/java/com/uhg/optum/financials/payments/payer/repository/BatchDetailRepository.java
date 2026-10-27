package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.dto.batch.CorrelatedBatchDetail;
import com.uhg.optum.financials.payments.payer.model.BatchDetail;
import jakarta.transaction.Transactional;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.List;

@Repository
public interface BatchDetailRepository extends JpaRepository<BatchDetail, Long> {

	List<BatchDetail> findByCorrelationId(String correlationId);

	boolean existsByCorrelationId(String correlationId);

	boolean existsByCorrelationIdAndBatchId(String correlationId, String batchId);

	@Query(value = "select sum(bd.number_of_payments) as number_of_payments , bd.correlation_id as correlation_id, sum(total_amount) as total_amount from virtual_card.batch_detail bd where bd.status = :status group by correlation_id",
			nativeQuery = true)
	List<CorrelatedBatchDetail> getCorrelationRecordForProcessing(String status);

	@Transactional
	@Modifying
	@Query("update BatchDetail set status = :status , updatedOn = CURRENT_TIMESTAMP where correlationId = :correlationId and status = :oldStatus")
	void updateBatchDetailStatus(String correlationId, String status, String oldStatus);

	@Query("SELECT SUM(bd.numberOfPayments) FROM BatchDetail bd WHERE bd.correlationId = :correlationId")
	Long getSumOfPaymentsByCorrelationId(String correlationId);

	@Query(value = "select * from virtual_card.batch_detail bd where bd.correlation_id = :correlationId limit 1",
			nativeQuery = true)
	BatchDetail findByCorrelationIdLimit1(String correlationId);

	@Query(value = "SELECT * FROM virtual_card.batch_detail where status = :batchStatus and date(created_on) = CAST(:enterDate AS DATE)",
			nativeQuery = true)
	List<BatchDetail> findAllByStatus(String batchStatus, String enterDate);

	@Transactional
	@Modifying
	@Query("update BatchDetail set status = 'P' , updatedOn = CURRENT_TIMESTAMP where correlationId = :correlationId")
	void updateStatusByCorrelationId(String correlationId);

	@Transactional
	@Modifying
	@Query("update BatchDetail set status = :status , " + "totalAmount = :totalAmount, "
			+ "numberOfPayments = :numberOfPayments, "
			+ "updatedOn = CURRENT_TIMESTAMP where correlationId = :correlationId and batchId = :batchId")
	int updateDetailsByCorrelationId(String status, BigDecimal totalAmount, Long numberOfPayments, String correlationId,
			String batchId);

	@Transactional
	@Modifying
	@Query("update BatchDetail set status = :status , " + "numberOfPayments = :numberOfPayments,"
			+ "updatedOn = CURRENT_TIMESTAMP where correlationId = :correlationId and batchId = :batchId")
	int updateDetailsByCorrelationId(String status, Long numberOfPayments, String correlationId, String batchId);

	@Transactional
	@Modifying
	@Query("update BatchDetail set status = :status , " + "totalAmount = :totalAmount, "
			+ "updatedOn = CURRENT_TIMESTAMP where correlationId = :correlationId and batchId = :batchId")
	int updateDetailsByCorrelationId(String status, BigDecimal totalAmount, String correlationId, String batchId);

	@Transactional
	@Modifying
	@Query("update BatchDetail set status = :status ,"
			+ "updatedOn = CURRENT_TIMESTAMP where correlationId = :correlationId and batchId = :batchId")
	int updateDetailsByCorrelationId(String status, String correlationId, String batchId);

	@Transactional
	@Modifying
	@Query("update BatchDetail set status = :status , updatedOn = CURRENT_TIMESTAMP where DATE(createdOn) >= :startDate and DATE(createdOn) <= :endDate")
	int updateStatusByDateRange(String status, LocalDateTime startDate, LocalDateTime endDate);

	@Query(value = "SELECT DISTINCT correlation_id, batch_id, created_on FROM virtual_card.batch_detail ORDER BY created_on DESC OFFSET :offset FETCH NEXT :limit ROWS ONLY",
			nativeQuery = true)
	List<String> getCorrelationIds(int offset, int limit);

}
