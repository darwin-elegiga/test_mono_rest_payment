package com.uhg.optum.financials.payments.payer.constant;

/**
 * Description: AppConstant.
 *
 * @author csingh36, created on 8-July-2024
 */
public final class JobConstant {

	private JobConstant() {
	}

	public static final String ST_SE_SEGMENT_CREATION = "ST_SE_SEGMENT_CREATION";

	public static final String FUNDING_FILE_CREATION = "FUNDING_FILE_CREATION";

	public static final String CONSOLIDATED_FILE_CREATION = "CONSOLIDATED_FILE_CREATION";

	public static final String TRANSFER_FILE = "TRANSFER_FILE";

	public static final String CLEANUP_DATA = "CLEANUP_DATA";

	public static final String NO_HANDLER_FOR_JOB_MSG = "No Handler found for job name: {}. TODO.";

	public static final String INVALID_JOB_MSG = "Invalid job name: {}. Valid job names are: {}";

	public static final String JOB_SUBMIT_SUCCESS_MESSAGE = "Job: {} submitted successfully.";

	public static final String JOB_SUBMIT_ERROR_MESSAGE = "Error while submitting job: {}.";

	public static final String JOB_ID = "JobID";

	public static final String ORIGINATING_COMPANY_ID = "ORIGINATINGCOMPANYID";

	public static final String FUNDING_DATA_KEY = "FUNDING_DATA";

	public static final String FILE_TO_TRANSFER = "FILE_TO_TRANSFER";

	public static final String PAYMENT_ID_KEY = "PAYMENT_IDS";

	public static final String SENT = "sent";

	public static final String REGEX_PIPE = "\\|";

	public static final String DATE_FORMAT_YYYY_MM_DD = "yyyy-MM-dd";

	public static final String KEY_ROTATION = "KEY_ROTATION";

	public static final String ECC_KEY_ROTATION = "ECC_KEY_ROTATION";

	public static final String KEY_NAME = "KEY";

	public static final String KEY_VALUE = "VALUE";

	public static final String BANK_ACCOUNT_KEY_PREFIX = "Bank_Account_";

	public static final String PAYMENT = "payment";

	public static final String FAILED = "failed";

	public static final String ERROR_CODE_3024 = "ERR3024";

	public static final String EOB_CREATION = "EOB_CREATION";

	public static final String EOB_ERROR_LOGS = "Error while creating EOB File for correlationID : ";

	public static final String PAYMENT_REQUEST_NOTFOUND_LOGS = "Payment request not found for payment id: ";

	public static final String CORRELATION_ID = "correlationId";

	public static final String EVENT_TYPE = "eventType";

	public static final String COMPLETED = "completed";

	public static final String CORRELATION_ID_NOT_FOUND = "No correlation record found for processing";

	public static final String FILE_NOT_FOUND = "No file found following correlation IDs ";

	public static final String AMOUNT_NOT_MATCH = "Total amount did not match for correlation id : ";

	public static final String PAYMENT_COUNT_NOT_MATCH = "Number of payment did not match for correlation id : ";

	public static final String EOB_FILE_TO_TRANSFER = "EOB_FILE_TO_TRANSFER";

	public static final String CORRELATION_IDS_KEY = "CORRELATION_IDS";

	public static final String EOB_TRANSFER_FILE = "EOB_TRANSFER_FILE";

	public static final Integer STATUS_UPDATE_BATCH_SIZE = 10000;

	public static final String PARAM = "jobParam";

	public static final String OLD_835_FILE_DATE = "old835FileDate";

	public static final String IS_REMIT = "remit";

	public static final String PREVIOUS_ENCRYPTION_KEY_AT_REST = "PREVIOUS_ENCRYPTION_KEY_AT_REST";

	public static final String LATEST_ENCRYPTION_KEY_AT_REST = "LATEST_ENCRYPTION_KEY_AT_REST";

	public static final String NETWORK_PAYMENT_ID_KEY = "NETWORK_PAYMENT_IDS";

	public static final String REMIT_C835_INCIDENT_CREATION = "REMIT_C835_INCIDENT_CREATION";

	public static final String ERROR = "error";

	public static final String INFO = "info";

	public static final String PIPE_DELIMITER = "|";

	public static final String CDEVC_URL = "/api/virtual-card-cde/";

}
