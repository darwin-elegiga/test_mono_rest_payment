package com.uhg.optum.financials.payments.payer.constant;

public final class DocumentConstants {

	private DocumentConstants() {
	}

	public static final String LOADING = "loading";

	public static final String PAYMENT = "payment";

	public static final String SETTLED = "settled";

	public static final String DOT_DELIMITER = ".";

	public static final String SLASH_DELIMITER = "/";

	public static final String UNDERSCORE_DELIMITER = "_";

	public static final String SUCCESS_FILE_EXTENSION = ".success";

	public static final String TXT_FILE_EXTENSION = ".txt";

	public static final String CTL_FILE_EXTENSION = ".ctl";

	public static final String GZIP_FILE_EXTENSION = ".gz";

	public static final String AP_SUCCESS_FILE_REGEX = "[A-Z0-9]+_AP[.]success";

	public static final String AP_TXT_FILE_REGEX = "[A-Z0-9]+_AP[0-9]{14}[.]txt";

	public static final String REAP_SUCCESS_FILE_REGEX = "[A-Z0-9]+_REAP[.]success";

	public static final String REAP_TXT_FILE_REGEX = "[A-Z0-9]+_REAP[0-9]{14}[.]txt";

	public static final String CONSOLIDATED_835_FILE_REGEX = "[A-Z0-9]+_[0-9]{8}[.]txt";

	public static final String TRN_REGEX = "~TRN\\*\\d+\\*([A-Z0-9_]+)\\*";

	public static final String COMPLETED = "completed";

	public static final String FAILED = "failed";

	public static final String DENIED = "denied";

	public static final String ERROR_CODE_3024 = "ERR3024";

	public static final String ACTIVITY = "activity";

	public static final String DUPLICATE_PAYMENT_EXCEPTION_MESSAGE = "payment identifier already exists";

	public static final String TENANT_HEADER = "X-Optum-Tenant-Id";

	public static String BLOBS_PATH = "blobs/";

	public static final String SIXTY_FIVE = "65";

	public static final String REM_ADV = "rem-adv";

	public static final String JOB_RUNNING_MSG = "Job Already Running";

	public static final String ERROR_SUBMITTING_JOB_MSG = "Error submitting job.";

	public static final String SUMMARY = "Summary_";

	public static final String C835_ERROR = "Error occurred while processing C835 with file path: ";

	public static final String AP_ERROR = "Error occurred while processing AP with file path: ";

	public static final String REAP_ERROR = "Error occurred while processing REAP with file path: ";

	public static final String AP_MESSAGE_ERROR = "Error occurred while processing AP Message ";

	public static final String REAP_MESSAGE_ERROR = "Error occurred while processing REAP Message ";

	public static final String SUMMARY_DOCUMENT_MESSAGE_ERROR = "Error occurred while generating summary document for network payment id: ";

	public static final String ERROR_MSG = " Error msg: ";

	public static final String COMPANION_FILE = "companionFile";

	public static final String JSON_EXTENSION = ".json";

	public static final String SUMMARY_PAGE_DOC_TYPE = "summary-page";

}
