package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.dto.CorrelatedPaymentDetail;
import com.uhg.optum.financials.payments.payer.dto.PaymentRequestTotalDto;
import com.uhg.optum.financials.payments.payer.dto.PaymentRequestStatusAmountCountDto;
import com.uhg.optum.financials.payments.payer.dto.RePreferenceCandidateGroup;
import com.uhg.optum.financials.payments.payer.dto.RePreferencePaymentRow;
import com.uhg.optum.financials.payments.payer.dto.WebhookPaymentDto;
import com.uhg.optum.financials.payments.payer.dto.document.SummaryDocumentInputDto;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import jakarta.transaction.Transactional;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;
import java.util.Set;

@Repository
public interface PaymentRequestRepository extends JpaRepository<PaymentRequest, Long> {

	@Query("SELECT COUNT(pr) > 0 FROM PaymentRequest pr WHERE pr.paymentDetails.id = :paymentId")
	boolean existsByPaymentDetailsId(@Param("paymentId") String paymentId);

	@Query("select pr from PaymentRequest pr where pr.paymentDetails.id = ?1")
	PaymentRequest findByPaymentId(String paymentId);

	Optional<PaymentRequest> findByPaymentDetailsNetworkPaymentId(String networkPaymentId);

	@EntityGraph(attributePaths = { "paymentDetails", "payeeDetails", "shippingDetails.address" })
	@Query("SELECT new com.uhg.optum.financials.payments.payer.dto.document.SummaryDocumentInputDto( pr.orgShortCode, pr.accountShortCode, pr.subAccountShortCode, pr.payeeDetails.name, "
			+ "pr.payeeDetails.taxId, pr.payeeDetails.npi, pr.payeeDetails.receiverId, pr.payeeDetails.type, "
			+ "pr.shippingDetails.address.line1, pr.shippingDetails.address.line2, pr.shippingDetails.address.city, pr.shippingDetails.address.state, pr.shippingDetails.address.country, pr.shippingDetails.address.postalCode, "
			+ "pr.paymentDetails.paymentDate, pr.paymentDetails.networkPaymentId) "
			+ " FROM PaymentRequest pr WHERE pr.paymentDetails.issuerPaymentId = :issuerPaymentId AND pr.correlationId = :correlationId")
	SummaryDocumentInputDto findDocDetailsByIssuerPaymentIdAndCorrelationId(String issuerPaymentId,
			String correlationId);

	@Query(value = "SELECT pr.status as status , COUNT(*) as numberOfPayments , SUM(pd.amount) AS totalAmount "
			+ "FROM virtual_card.payment_request pr "
			+ "INNER JOIN virtual_card.payment_details pd ON pd.id = pr.payment_details_id "
			+ "WHERE pr.correlation_id = :correlationId GROUP BY pr.status", nativeQuery = true)
	List<PaymentRequestStatusAmountCountDto> findPaymentStatusByCorrelationIdGroupByStatus(
			@Param("correlationId") String correlationId);

	@Modifying
	@Transactional
	@Query("""
			UPDATE PaymentRequest pr
			SET pr.stSeSegment = NULL
			WHERE pr.correlationId = :correlationId
			AND pr.stSeSegment IS NOT NULL
			AND pr.status = :paymentStatus
			""")
	int nullifyStSeSegmentForCorrelationId(@Param("correlationId") String correlationId,
			@Param("paymentStatus") String status);

	@Query("SELECT new com.uhg.optum.financials.payments.payer.dto.WebhookPaymentDto("
			+ "pr.paymentDetails.id, pr.paymentDetails.networkPaymentId, pr.paymentDetails.amount, pr.orgShortCode, pr.metadata) "
			+ "FROM PaymentRequest pr WHERE pr.correlationId = :correlationId")
	Page<WebhookPaymentDto> findByCorrelationId(@Param("correlationId") String correlationId, Pageable pageable);

	@Query(value = "SELECT pr.status as status, COUNT(*) as numberOfPayments, SUM(pd.amount) as totalAmount "
			+ "FROM virtual_card.payment_request pr "
			+ "INNER JOIN virtual_card.payment_details pd ON pd.id = pr.payment_details_id "
			+ "WHERE pr.correlation_id = :correlationId GROUP BY pr.status", nativeQuery = true)
	List<PaymentRequestStatusAmountCountDto> findPaymentStatsByCorrelationIdGroupByStatus(
			@Param("correlationId") String correlationId);

	@Transactional
	@Modifying(clearAutomatically = true, flushAutomatically = true)
	@Query("update PaymentRequest pr set pr.status= ?1, pr.updatedOn = CURRENT_TIMESTAMP where pr.id in (?2)")
	void updateStatusByIdPaymentIdIn(String status, Set<String> paymentRequestIds);

	@Query(value = "select pr.correlation_id, count(*) as number_of_payments, sum(pd.amount) as total_amount "
			+ "from virtual_card.payment_request pr inner join virtual_card.payment_details pd on pd.id = pr.payment_details_id "
			+ "inner join virtual_card.virtual_cards vc on vc.payment_id = pd.id "
			+ "group  by pr.correlation_id, pr.status, vc.status having pr.correlation_id = :correlationId and pr.status = :status and vc.status = :cardStatus",
			nativeQuery = true)
	CorrelatedPaymentDetail findCorrelatedPaymentRequest(String correlationId, String status, String cardStatus);

	@Query(value = "SELECT pr.org_short_code FROM virtual_card.payment_request pr WHERE pr.correlation_id = :correlationId LIMIT 1",
			nativeQuery = true)
	String findOrgByCorrelationId(String correlationId);

	@Query(value = """
			SELECT DISTINCT pr.correlation_id
			FROM virtual_card.batch_detail bd
			JOIN virtual_card.payment_request pr ON pr.correlation_id = bd.correlation_id
			JOIN virtual_card.payment_details pd ON pd.id = pr.payment_details_id
			JOIN virtual_card.virtual_cards vc ON vc.payment_id = pd.id
			JOIN virtual_card.file_tracking ft ON ft.correlation_id = pr.correlation_id
			WHERE bd.status = :batchStatus
			  AND pr.status = :paymentStatus
			  AND vc.status = :cardStatus
			  AND NULLIF(BTRIM(ft."835"), '') IS NOT NULL
			  AND NULLIF(BTRIM(ft.companion), '') IS NULL
			  AND NOT EXISTS (
			      SELECT 1
			      FROM virtual_card.payment_request missing_pr
			      JOIN virtual_card.payment_details missing_pd ON missing_pd.id = missing_pr.payment_details_id
			      WHERE missing_pr.correlation_id = pr.correlation_id
			        AND NOT EXISTS (
			            SELECT 1
			            FROM virtual_card.document_response_details drd
			            WHERE drd.network_payment_id = missing_pd.network_payment_id
			              AND NULLIF(BTRIM(drd.document_url), '') IS NOT NULL
			        )
			  )
			""", nativeQuery = true)
	List<String> findEligibleCompanionCorrelationIds(@Param("batchStatus") String batchStatus,
			@Param("paymentStatus") String paymentStatus, @Param("cardStatus") String cardStatus);

	@Query(value = """
			SELECT COUNT(*) = 0
			FROM virtual_card.payment_request pr
			JOIN virtual_card.payment_details pd ON pd.id = pr.payment_details_id
			WHERE pr.correlation_id = :correlationId
			  AND NOT EXISTS (
			      SELECT 1
			      FROM virtual_card.document_response_details drd
			      WHERE drd.network_payment_id = pd.network_payment_id
			        AND NULLIF(BTRIM(drd.document_url), '') IS NOT NULL
			  )
			""", nativeQuery = true)
	boolean hasSummaryPageForEveryPaymentByCorrelationId(@Param("correlationId") String correlationId);

	List<PaymentRequest> findByCorrelationId(String correlationId);

	@Query("SELECT new com.uhg.optum.financials.payments.payer.dto.PaymentRequestTotalDto$PaymentRequest("
			+ "pr.status, pr.paymentDetails.id, pr.paymentDetails.amount) "
			+ "FROM PaymentRequest pr WHERE pr.correlationId = :correlationId")
	List<PaymentRequestTotalDto.PaymentRequest> findPaymentRequestsByCorrelationId(
			@Param("correlationId") String correlationId);

	@Query(value = "SELECT COUNT(*) AS record_count FROM virtual_card.payment_request WHERE correlation_id = :correlationId AND status = :status",
			nativeQuery = true)
	int countPaymentRequestByCorrelationIdAndStatus(@Param("correlationId") String correlationId,
			@Param("status") String status);

	@EntityGraph(attributePaths = { "paymentDetails", "payeeDetails", "payeeDetails.address", "shippingDetails",
			"shippingDetails.address" })
	Page<PaymentRequest> findAllByCorrelationIdAndStatus(String correlationId, String status, Pageable pageable);

	/**
	 * Keyset ("seek") pagination for the bulk correlation reprocess sweep. Plain
	 * offset-based {@link Pageable} paging over this same filter is unsafe here because
	 * {@code status} — the very column being filtered on — is mutated by the dispatches
	 * this sweep triggers while the sweep is still running; a shrinking filtered set
	 * shifts every later offset, causing rows to be skipped entirely. Filtering on
	 * {@code id > :lastSeenId} instead of an offset is immune to that: it is unaffected
	 * by rows elsewhere in the set changing status.
	 */
	@EntityGraph(attributePaths = { "paymentDetails", "payeeDetails", "payeeDetails.address", "shippingDetails",
			"shippingDetails.address" })
	List<PaymentRequest> findByCorrelationIdAndStatusAndIdGreaterThanOrderByIdAsc(String correlationId, String status,
			Long lastSeenId, Pageable pageable);

	@EntityGraph(attributePaths = { "paymentDetails", "payeeDetails", "shippingDetails" })
	List<PaymentRequest> findByIdIn(Set<String> paymentRequestIds);

	@Query(value = """
			SELECT pr.org_short_code AS orgShortCode,
				pr.correlation_id AS correlationId
			FROM virtual_card.payment_request pr
			LEFT JOIN virtual_card.re_preference_job_failure f ON f.payment_request_id = pr.id
			WHERE pr.re_preference_flag = TRUE
				AND pr.status = 'PI'
				AND pr.re_preference_processed = FALSE
				AND COALESCE(f.attempt_count, 0) < :maxAttempts
			GROUP BY pr.org_short_code, pr.correlation_id
			""", nativeQuery = true)
	List<RePreferenceCandidateGroup> findRePreferenceCandidateGroups(@Param("maxAttempts") int maxAttempts);

	@Query(value = """
			SELECT pr.id AS id, pd.amount AS amount
			FROM virtual_card.payment_request pr
			INNER JOIN virtual_card.payment_details pd ON pd.id = pr.payment_details_id
			LEFT JOIN virtual_card.re_preference_job_failure f ON f.payment_request_id = pr.id
			WHERE pr.org_short_code = :orgShortCode
				AND pr.correlation_id = :correlationId
				AND pr.re_preference_flag = TRUE
				AND pr.status = 'PI'
				AND pr.re_preference_processed = FALSE
				AND COALESCE(f.attempt_count, 0) < :maxAttempts
			""", nativeQuery = true)
	List<RePreferencePaymentRow> findRePreferencePayments(@Param("orgShortCode") String orgShortCode,
			@Param("correlationId") String correlationId, @Param("maxAttempts") int maxAttempts);

	@Transactional
	@Modifying(clearAutomatically = true, flushAutomatically = true)
	@Query(value = """
			UPDATE virtual_card.payment_request
			SET correlation_id = :newCorrelationId,
				re_preference_processed = TRUE,
				updated_on = CURRENT_TIMESTAMP,
				updated_by_id = :updatedById
			WHERE id IN (:paymentRequestIds)
				AND re_preference_flag = TRUE
				AND re_preference_processed = FALSE
			""", nativeQuery = true)
	int updateCorrelationAndMarkRePreferenceProcessed(@Param("paymentRequestIds") List<Long> paymentRequestIds,
			@Param("newCorrelationId") String newCorrelationId, @Param("updatedById") String updatedById);

	@Transactional
	@Modifying(clearAutomatically = true, flushAutomatically = true)
	@Query(value = """
			UPDATE virtual_card.payment_request
			SET re_preference_processed = TRUE,
				updated_on = CURRENT_TIMESTAMP,
				updated_by_id = :updatedById
			WHERE id IN (:paymentRequestIds)
				AND re_preference_flag = TRUE
				AND re_preference_processed = FALSE
			""", nativeQuery = true)
	int markRePreferenceProcessed(@Param("paymentRequestIds") List<Long> paymentRequestIds,
			@Param("updatedById") String updatedById);

}
