package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.config.JobNameProviderConfig;
import com.uhg.optum.financials.payments.payer.constant.JobName;
import com.uhg.optum.financials.payments.payer.job.CompanionFileJob;
import com.uhg.optum.financials.payments.payer.job.ECCKeyRotationJob;
import com.uhg.optum.financials.payments.payer.job.Payment835Job;
import com.uhg.optum.financials.payments.payer.job.RePreferenceCorrelationJob;
import com.uhg.optum.financials.payments.payer.job.VCCReportsJob;
import com.uhg.optum.financials.payments.payer.service.JobService;
import com.uhg.optum.financials.payments.payer.util.AuditUtil;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.util.Arrays;
import java.util.Objects;

import static com.uhg.optum.financials.payments.payer.constant.JobConstant.INVALID_JOB_MSG;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.JOB_SUBMIT_ERROR_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.JOB_SUBMIT_SUCCESS_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.NO_HANDLER_FOR_JOB_MSG;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.DEBUG;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;

@Slf4j
@Service
public class JobServiceImpl implements JobService {

	private final JobNameProviderConfig jobNameProviderConfig;

	private final Payment835Job payment835Job;

	private final LogUtility logUtility;

	private final AuditUtil auditUtil;

	private final ECCKeyRotationJob eccKeyRotationJob;

	private final CompanionFileJob companionFileJob;

	private final RePreferenceCorrelationJob rePreferenceCorrelationJob;

	private final VCCReportsJob vccReportsJob;

	public JobServiceImpl(JobNameProviderConfig jobNameProviderConfig, Payment835Job payment835Job, AuditUtil auditUtil,
			LogUtility logUtility, ECCKeyRotationJob eccKeyRotationJob, CompanionFileJob c835CompanionJob,
			VCCReportsJob vccReportsJob, RePreferenceCorrelationJob rePreferenceCorrelationJob) {
		this.jobNameProviderConfig = jobNameProviderConfig;
		this.payment835Job = payment835Job;
		this.logUtility = logUtility;
		this.auditUtil = auditUtil;
		this.eccKeyRotationJob = eccKeyRotationJob;
		this.companionFileJob = c835CompanionJob;
		this.vccReportsJob = vccReportsJob;
		this.rePreferenceCorrelationJob = rePreferenceCorrelationJob;
	}

	@Override
	public void checkAndSubmitJob() {
		for (String argJobName : this.jobNameProviderConfig.getName()) {
			JobName jobName = JobName.toJob(argJobName);
			if (Objects.isNull(jobName)) {
				this.logInvalidJob(argJobName);
				continue;
			}
			this.submitJob(argJobName, jobName);
		}
	}

	private void logInvalidJob(String argJobName) {
		this.logUtility.logMessage(INVALID_JOB_MSG + argJobName + Arrays.toString(JobName.values()), INFO, log);
	}

	private void submitJob(String arg, JobName jobName) {
		this.logUtility.logMessage("**** Submitting job: " + jobName + " ****", INFO, log);
		try {
			switch (jobName) {
				case PAYMENT_835_CREATOR_JOB:
					this.payment835Job.run();
					break;
				case COMPANION_FILE_JOB:
					this.companionFileJob.run();
					break;
				case RE_PREFERENCE_CORRELATION_JOB:
					this.rePreferenceCorrelationJob.run();
					break;
				case ECC_KEY_ROTATION_JOB:
					this.eccKeyRotationJob.run();
					break;
				case POSTED_FILE_CREATION_JOB:
					this.vccReportsJob.generatePostedFile();
					break;
				case VCC_REPORTS_NON_FINANCIAL_JOB:
					this.vccReportsJob.generateVCCNonFinancialFile();
					break;
				case AUTHORIZED_FILE_CREATION_JOB:
					this.vccReportsJob.generateAuthorizedFile();
					break;
				default:
					this.logUtility.logMessage(NO_HANDLER_FOR_JOB_MSG + arg, INFO, log);
					return;
			}
			this.logUtility.logMessage(JOB_SUBMIT_SUCCESS_MESSAGE + jobName, DEBUG, log);
		}
		catch (Exception ex) {
			this.logUtility.logMessage(JOB_SUBMIT_ERROR_MESSAGE + jobName + " " + ex.getMessage(), ERROR, log);

			this.auditUtil.sendErrorEvent(JOB_SUBMIT_ERROR_MESSAGE + jobName + ex.getMessage());
		}
	}

}
