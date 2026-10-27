package com.uhg.optum.financials.payments.payer.impl;

import com.uhg.optum.financials.payments.payer.config.JobNameProviderConfig;
import com.uhg.optum.financials.payments.payer.job.Payment835Job;
import com.uhg.optum.financials.payments.payer.job.RePreferenceCorrelationJob;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import com.uhg.optum.financials.payments.payer.service.impl.JobServiceImpl;
import com.uhg.optum.financials.payments.payer.util.AuditUtil;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Collections;

@ExtendWith(MockitoExtension.class)
class JobServiceImplTests {

	@InjectMocks
	private JobServiceImpl jobServiceImpl;

	@Mock
	private JobNameProviderConfig jobNameProviderConfig;

	@Mock
	private Payment835Job payment835Job;

	@Mock
	private LogUtility logUtility;

	@Mock
	private AuditUtil auditUtil;

	@Mock
	private RePreferenceCorrelationJob rePreferenceCorrelationJob;

	@Mock
	private BatchDetailService batchDetailService;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@ParameterizedTest
	@ValueSource(strings = { "PAYMENT_SEGMENT_CREATOR_JOB", "PAYMENT_835_CREATOR_JOB", "RE_PREFERENCE_CORRELATION_JOB",
			"REMIT_835_CREATOR_JOB", "KEY_ROTATION_JOB", "EOB_GENERATION_JOB", "ECC_KEY_ROTATION_JOB",
			"C835_COMPANION_JOB", "ECG_FILE_PULL_JOB", "ENCRYPTION_AT_REST_KEY_ROTATION_JOB", "INVALID_JOB", "" })
	void testCheckAndSubmitJobSegmentCreator(String jobName) {
		Mockito.when(this.jobNameProviderConfig.getName()).thenReturn(Collections.singletonList(jobName));
		this.jobServiceImpl.checkAndSubmitJob();
		Assertions.assertThat(this.jobServiceImpl).isNotNull();
	}

	@Test
	void testCheckAndSubmitJobKeyRotation() {
		Mockito.when(this.jobNameProviderConfig.getName()).thenReturn(Collections.singletonList("KEY_ROTATION_JOB"));
		this.jobServiceImpl.checkAndSubmitJob();
		Assertions.assertThat(this.jobServiceImpl).isNotNull();
	}

}
