package com.uhg.optum.financials.payments.payer.job;

import com.uhg.optum.financials.payments.payer.dto.RePreferenceCandidateGroup;
import com.uhg.optum.financials.payments.payer.dto.RePreferencePaymentRow;
import com.uhg.optum.financials.payments.payer.exception.JobAlreadyRunningException;
import com.uhg.optum.financials.payments.payer.job.service.RePreferenceCorrelationService;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.RePreferenceJobFailureRepository;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.batch.core.job.JobExecution;
import org.springframework.batch.core.job.JobInstance;
import org.springframework.batch.core.repository.JobRepository;

import java.math.BigDecimal;
import java.util.Collections;
import java.util.List;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.RE_PREFERENCE_MAX_ATTEMPTS;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.anyInt;
import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.willThrow;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;

@ExtendWith(MockitoExtension.class)
class RePreferenceCorrelationJobTests {

	@InjectMocks
	private RePreferenceCorrelationJob job;

	@Mock
	private JobRepository jobRepository;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@Mock
	private RePreferenceJobFailureRepository rePreferenceJobFailureRepository;

	@Mock
	private RePreferenceCorrelationService rePreferenceCorrelationService;

	@Mock
	private LogUtility logUtility;

	@BeforeEach
	void stubJobRepository() {
		given(this.jobRepository.getLastJobInstance(anyString())).willReturn(null);
	}

	@Test
	void run_returnsJobId_whenNoCandidateGroupsFound() {
		given(this.paymentRequestRepository.findRePreferenceCandidateGroups(RE_PREFERENCE_MAX_ATTEMPTS))
			.willReturn(Collections.emptyList());

		String jobId = this.job.run();

		assertThat(jobId).isNotBlank();
		verify(this.rePreferenceCorrelationService, never()).processGroup(anyString(), anyString());
	}

	@Test
	void run_processesEachGroup_whenCandidateGroupsExist() {
		RePreferenceCandidateGroup group = candidateGroup("ORG1", "CORR1");
		given(this.paymentRequestRepository.findRePreferenceCandidateGroups(RE_PREFERENCE_MAX_ATTEMPTS))
			.willReturn(List.of(group));

		String jobId = this.job.run();

		assertThat(jobId).isNotBlank();
		verify(this.rePreferenceCorrelationService).processGroup("ORG1", "CORR1");
		verify(this.rePreferenceJobFailureRepository, never()).upsertFailure(anyLong(), anyString());
	}

	@Test
	void run_processesAllGroups_whenMultipleCandidateGroupsExist() {
		RePreferenceCandidateGroup group1 = candidateGroup("ORG1", "CORR1");
		RePreferenceCandidateGroup group2 = candidateGroup("ORG2", "CORR2");
		given(this.paymentRequestRepository.findRePreferenceCandidateGroups(RE_PREFERENCE_MAX_ATTEMPTS))
			.willReturn(List.of(group1, group2));

		this.job.run();

		verify(this.rePreferenceCorrelationService).processGroup("ORG1", "CORR1");
		verify(this.rePreferenceCorrelationService).processGroup("ORG2", "CORR2");
	}

	@Test
	void run_recordsFailureForEachPayment_whenGroupProcessingThrows() {
		RePreferenceCandidateGroup group = candidateGroup("ORG1", "CORR1");
		given(this.paymentRequestRepository.findRePreferenceCandidateGroups(RE_PREFERENCE_MAX_ATTEMPTS))
			.willReturn(List.of(group));

		RuntimeException cause = new RuntimeException("DB timeout");
		willThrow(cause).given(this.rePreferenceCorrelationService).processGroup("ORG1", "CORR1");

		RePreferencePaymentRow row1 = paymentRow(101L, new BigDecimal("100.00"));
		RePreferencePaymentRow row2 = paymentRow(102L, new BigDecimal("200.00"));
		given(this.paymentRequestRepository.findRePreferencePayments(eq("ORG1"), eq("CORR1"),
				eq(RE_PREFERENCE_MAX_ATTEMPTS)))
			.willReturn(List.of(row1, row2));

		this.job.run();

		verify(this.rePreferenceJobFailureRepository).upsertFailure(101L, "java.lang.RuntimeException: DB timeout");
		verify(this.rePreferenceJobFailureRepository).upsertFailure(102L, "java.lang.RuntimeException: DB timeout");
	}

	@Test
	void run_continuesProcessingNextGroup_whenOneGroupFails() {
		RePreferenceCandidateGroup group1 = candidateGroup("ORG1", "CORR1");
		RePreferenceCandidateGroup group2 = candidateGroup("ORG2", "CORR2");
		given(this.paymentRequestRepository.findRePreferenceCandidateGroups(RE_PREFERENCE_MAX_ATTEMPTS))
			.willReturn(List.of(group1, group2));

		willThrow(new RuntimeException("error")).given(this.rePreferenceCorrelationService)
			.processGroup("ORG1", "CORR1");
		given(this.paymentRequestRepository.findRePreferencePayments(eq("ORG1"), eq("CORR1"),
				eq(RE_PREFERENCE_MAX_ATTEMPTS)))
			.willReturn(Collections.emptyList());

		this.job.run();

		verify(this.rePreferenceCorrelationService).processGroup("ORG2", "CORR2");
	}

	@Test
	void run_recordsNoFailure_whenGroupFailsButNoPaymentsReturned() {
		RePreferenceCandidateGroup group = candidateGroup("ORG1", "CORR1");
		given(this.paymentRequestRepository.findRePreferenceCandidateGroups(RE_PREFERENCE_MAX_ATTEMPTS))
			.willReturn(List.of(group));

		willThrow(new RuntimeException("error")).given(this.rePreferenceCorrelationService)
			.processGroup("ORG1", "CORR1");
		given(this.paymentRequestRepository.findRePreferencePayments(anyString(), anyString(), anyInt()))
			.willReturn(Collections.emptyList());

		this.job.run();

		verify(this.rePreferenceJobFailureRepository, never()).upsertFailure(anyLong(), anyString());
	}

	@Test
	void run_throwsJobAlreadyRunningException_whenJobIsAlreadyRunning() {
		JobInstance jobInstance = mock(JobInstance.class);
		JobExecution runningExecution = mock(JobExecution.class);
		given(runningExecution.isRunning()).willReturn(true);
		given(this.jobRepository.getLastJobInstance(anyString())).willReturn(jobInstance);
		given(this.jobRepository.getJobExecutions(jobInstance)).willReturn(List.of(runningExecution));

		assertThatThrownBy(() -> this.job.run()).isInstanceOf(JobAlreadyRunningException.class);
	}

	private static RePreferenceCandidateGroup candidateGroup(String org, String correlationId) {
		return new RePreferenceCandidateGroup() {
			@Override
			public String getOrgShortCode() {
				return org;
			}

			@Override
			public String getCorrelationId() {
				return correlationId;
			}
		};
	}

	private static RePreferencePaymentRow paymentRow(Long id, BigDecimal amount) {
		return new RePreferencePaymentRow() {
			@Override
			public Long getId() {
				return id;
			}

			@Override
			public BigDecimal getAmount() {
				return amount;
			}
		};
	}

}
