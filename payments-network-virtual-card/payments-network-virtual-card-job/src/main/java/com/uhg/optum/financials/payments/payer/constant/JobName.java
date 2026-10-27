package com.uhg.optum.financials.payments.payer.constant;

public enum JobName {

	PAYMENT_SEGMENT_CREATOR_JOB, PAYMENT_835_CREATOR_JOB, RE_PREFERENCE_CORRELATION_JOB, REMIT_835_CREATOR_JOB,
	KEY_ROTATION_JOB, EOB_GENERATION_JOB, ECC_KEY_ROTATION_JOB, C835_COMPANION_JOB, ECG_FILE_PULL_JOB,
	ENCRYPTION_AT_REST_KEY_ROTATION_JOB, POSTED_FILE_CREATION_JOB, COMPANION_FILE_JOB, VCC_REPORTS_NON_FINANCIAL_JOB,
	AUTHORIZED_FILE_CREATION_JOB;

	public static JobName toJob(String jobName) {
		for (JobName job : JobName.values()) {
			if (job.name().equalsIgnoreCase(jobName)) {
				return job;
			}
		}
		return null;
	}

}
