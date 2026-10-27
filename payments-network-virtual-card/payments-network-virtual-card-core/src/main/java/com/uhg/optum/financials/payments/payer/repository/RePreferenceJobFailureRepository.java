package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.RePreferenceJobFailure;
import jakarta.transaction.Transactional;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

@Repository
public interface RePreferenceJobFailureRepository extends JpaRepository<RePreferenceJobFailure, Long> {

	@Transactional
	@Modifying(clearAutomatically = true, flushAutomatically = true)
	@Query(value = """
			INSERT INTO virtual_card.re_preference_job_failure
				(payment_request_id, attempt_count, last_failed_on, last_error)
			VALUES (:paymentRequestId, 1, CURRENT_TIMESTAMP, :lastError)
			ON CONFLICT (payment_request_id)
			DO UPDATE SET
				attempt_count  = virtual_card.re_preference_job_failure.attempt_count + 1,
				last_failed_on = CURRENT_TIMESTAMP,
				last_error     = EXCLUDED.last_error
			""", nativeQuery = true)
	void upsertFailure(@Param("paymentRequestId") Long paymentRequestId, @Param("lastError") String lastError);

}
