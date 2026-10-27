package com.uhg.optum.financials.payments.payer.job.service;

import com.uhg.optum.financials.payments.payer.dto.RePreferencePaymentRow;
import com.uhg.optum.financials.payments.payer.model.BatchDetail;
import com.uhg.optum.financials.payments.payer.model.CorrelationMapping;
import com.uhg.optum.financials.payments.payer.repository.BatchDetailRepository;
import com.uhg.optum.financials.payments.payer.repository.CorrelationMappingRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.List;
import java.util.Objects;
import java.util.UUID;
import java.util.function.Predicate;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BATCH_DETAIL_STATUS_INITIALIZE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.RE_PREFERENCE_MAX_ATTEMPTS;

@Service
public class RePreferenceCorrelationService {

	private static final String INTERNAL_CORRELATION_PREFIX = "RRI";

	private static final String SYNTHETIC_BATCH_PREFIX = "RRF";

	private static final int RANDOM_PART_LENGTH = 29;

	private static final String UPDATED_BY_ID = "RE_PREFERENCE_CORRELATION_JOB";

	private final PaymentRequestRepository paymentRequestRepository;

	private final BatchDetailRepository batchDetailRepository;

	private final CorrelationMappingRepository correlationMappingRepository;

	public RePreferenceCorrelationService(PaymentRequestRepository paymentRequestRepository,
			BatchDetailRepository batchDetailRepository, CorrelationMappingRepository correlationMappingRepository) {
		this.paymentRequestRepository = paymentRequestRepository;
		this.batchDetailRepository = batchDetailRepository;
		this.correlationMappingRepository = correlationMappingRepository;
	}

	@Transactional
	public void processGroup(String orgShortCode, String actualCorrelationId) {
		List<RePreferencePaymentRow> rePrefRows = this.paymentRequestRepository.findRePreferencePayments(orgShortCode,
				actualCorrelationId, RE_PREFERENCE_MAX_ATTEMPTS);
		if (rePrefRows.isEmpty()) {
			return;
		}

		List<Long> paymentRequestIds = rePrefRows.stream().map(RePreferencePaymentRow::getId).toList();
		long paymentCount = paymentRequestIds.size();
		BigDecimal totalAmount = rePrefRows.stream()
			.map((row) -> (row.getAmount() == null) ? BigDecimal.ZERO : row.getAmount())
			.reduce(BigDecimal.ZERO, BigDecimal::add);

		// Always generate a new internal correlation ID for re-referenced payments
		String internalCorrelationId = generateUniqueCorrelation(INTERNAL_CORRELATION_PREFIX,
				(candidate) -> !this.correlationMappingRepository.existsById(candidate));
		CorrelationMapping mapping = new CorrelationMapping();
		mapping.setActualCorrelationId(actualCorrelationId);
		mapping.setInternalCorrelationId(internalCorrelationId);
		mapping.setCreatedOn(LocalDateTime.now());
		this.correlationMappingRepository.save(mapping);

		// Update all payments to use the new internal correlation and mark as processed
		int updatedCount = this.paymentRequestRepository
			.updateCorrelationAndMarkRePreferenceProcessed(paymentRequestIds, internalCorrelationId, UPDATED_BY_ID);
		assertUpdatedRowCount(rePrefRows.size(), updatedCount, actualCorrelationId);

		String syntheticBatchId = generateUniqueCorrelation(SYNTHETIC_BATCH_PREFIX,
				(candidate) -> !this.batchDetailRepository.existsByCorrelationIdAndBatchId(internalCorrelationId,
						candidate));

		BatchDetail batchDetail = new BatchDetail();
		batchDetail.setOrg(orgShortCode);
		batchDetail.setCorrelationId(internalCorrelationId);
		batchDetail.setBatchId(syntheticBatchId);
		batchDetail.setNumberOfPayments(paymentCount);
		batchDetail.setTotalAmount(totalAmount);
		batchDetail.setStatus(BATCH_DETAIL_STATUS_INITIALIZE);
		batchDetail.setCreatedOn(LocalDateTime.now());
		this.batchDetailRepository.saveAndFlush(batchDetail);
	}

	private String generateUniqueCorrelation(String prefix, Predicate<String> isUnique) {
		for (int i = 0; i < 10; i++) {
			String candidate = prefix + generateRandomPart();
			if (isUnique.test(candidate)) {
				return candidate;
			}
		}
		throw new IllegalStateException("Unable to generate unique id for prefix: " + prefix);
	}

	private String generateRandomPart() {
		// Generate UUID and remove hyphens to get 32 hex characters
		String uuidHex = UUID.randomUUID().toString().replace("-", "");
		// Take first 29 characters from UUID
		return uuidHex.substring(0, RANDOM_PART_LENGTH);
	}

	private void assertUpdatedRowCount(int expected, int actual, String correlationId) {
		if (!Objects.equals(expected, actual)) {
			throw new IllegalStateException("Unexpected update count for correlationId=" + correlationId + " expected="
					+ expected + " actual=" + actual);
		}
	}

}
