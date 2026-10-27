package com.uhg.optum.financials.payments.payer.job;

import com.uhg.optum.financials.payments.payer.dto.RePreferenceCandidateGroup;
import com.uhg.optum.financials.payments.payer.dto.RePreferencePaymentRow;
import com.uhg.optum.financials.payments.payer.job.service.RePreferenceCorrelationService;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.RePreferenceJobFailureRepository;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.springframework.batch.core.repository.JobRepository;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.RE_PREFERENCE_MAX_ATTEMPTS;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;

@Slf4j
@Component
public class RePreferenceCorrelationJob implements BaseJob {

	private static final String UPDATED_BY_ID = "RE_PREFERENCE_CORRELATION_JOB";

	private final JobRepository jobRepository;

	private final PaymentRequestRepository paymentRequestRepository;

	private final RePreferenceJobFailureRepository rePreferenceJobFailureRepository;

	private final RePreferenceCorrelationService rePreferenceCorrelationService;

	private final LogUtility logUtility;

	public RePreferenceCorrelationJob(JobRepository jobRepository, PaymentRequestRepository paymentRequestRepository,
			RePreferenceJobFailureRepository rePreferenceJobFailureRepository,
			RePreferenceCorrelationService rePreferenceCorrelationService, LogUtility logUtility) {
		this.jobRepository = jobRepository;
		this.paymentRequestRepository = paymentRequestRepository;
		this.rePreferenceJobFailureRepository = rePreferenceJobFailureRepository;
		this.rePreferenceCorrelationService = rePreferenceCorrelationService;
		this.logUtility = logUtility;
	}

	@Override
	public synchronized String run(Object... args) {
		String jobId = String.valueOf(System.currentTimeMillis());
		this.checkJobAlreadyRunningOrNot(this.jobRepository,
				com.uhg.optum.financials.payments.payer.constant.JobName.RE_PREFERENCE_CORRELATION_JOB);
		List<RePreferenceCandidateGroup> groups = this.paymentRequestRepository
			.findRePreferenceCandidateGroups(RE_PREFERENCE_MAX_ATTEMPTS);
		this.logUtility.logMessage("Re-preference job found candidate groups: " + groups.size(), INFO, log);
		for (RePreferenceCandidateGroup group : groups) {
			try {
				String correlationId = group.getCorrelationId();
				String orgShortCode = group.getOrgShortCode();
				this.rePreferenceCorrelationService.processGroup(orgShortCode, correlationId);
			}
			catch (Exception ex) {
				String reason = ex.getClass().getName() + ((ex.getMessage() != null) ? ": " + ex.getMessage() : "");
				this.logUtility.logMessage("Re-preference processing failed for correlationId="
						+ group.getCorrelationId() + " reason=" + reason, ERROR, log);
				this.recordFailures(group, ex);
			}
		}
		return jobId;
	}

	private void recordFailures(RePreferenceCandidateGroup group, Exception ex) {
		String errorMessage = ex.getClass().getName() + ((ex.getMessage() != null) ? ": " + ex.getMessage() : "");
		List<RePreferencePaymentRow> payments = this.paymentRequestRepository
			.findRePreferencePayments(group.getOrgShortCode(), group.getCorrelationId(), RE_PREFERENCE_MAX_ATTEMPTS);
		List<Long> allFailedIds = payments.stream().map(RePreferencePaymentRow::getId).toList();
		for (RePreferencePaymentRow payment : payments) {
			this.rePreferenceJobFailureRepository.upsertFailure(payment.getId(), errorMessage);
		}
		// After incrementing attempt_count, re-query to find payments that are still
		// eligible (attempt_count < MAX_ATTEMPTS). Those that dropped off have hit the
		// retry ceiling — mark them as processed so they are not picked up again.
		Set<Long> stillEligibleIds = this.paymentRequestRepository
			.findRePreferencePayments(group.getOrgShortCode(), group.getCorrelationId(), RE_PREFERENCE_MAX_ATTEMPTS)
			.stream()
			.map(RePreferencePaymentRow::getId)
			.collect(Collectors.toSet());
		List<Long> exhaustedIds = allFailedIds.stream().filter((id) -> !stillEligibleIds.contains(id)).toList();
		if (!exhaustedIds.isEmpty()) {
			this.paymentRequestRepository.markRePreferenceProcessed(exhaustedIds, UPDATED_BY_ID);
			this.logUtility.logMessage("Re-preference max retries (" + RE_PREFERENCE_MAX_ATTEMPTS
					+ ") exhausted — marking " + exhaustedIds.size() + " payment(s) as processed for correlationId="
					+ group.getCorrelationId(), INFO, log);
		}
	}

}
