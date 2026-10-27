package com.uhg.optum.financials.payments.payer.constant;

public final class ApiConstants {

	public static final String SUCCESS = "success";

	public static final String FAILED = "failed";

	public static final String INFO = "info";

	public static final String PAYMENT_LOAD_EXCEPTION_LOG = "Exception occurred while loading payment";

	public static final String CORRELATION_EVENT_EXCEPTION_LOG = "Exception occurred while sending correlation event";

	public static final String CREATE_CARD_WEBHOOK_PROCESS_EXCEPTION_LOG = "Exception occurred while processing create card webhook";

	public static final String CANCEL_CARD_WEBHOOK_PROCESS_EXCEPTION_LOG = "Exception occurred while processing cancel card webhook";

	public static final String PAYMENT_MSG_EXCEPTION_LOG = "Exception while sending the payment message to the queue: ";

	public static final String CORRELATION_MSG_EXCEPTION_LOG = "Exception while sending the correlation message to the queue: ";

	public static final String PAYMENT_DETAILS_NOTFOUND_LOG = "No payment detail found for paymentId ";

	public static final String FILE_NOT_FOUND_LOG = "File not found";

	public static final String FILE_PATH_REQUIRED_LOG = "filePath is required";

	public static final String INVALID_FILE_PATH_LOG = "Invalid file path";

	public static final String X_OPTUM_TENANT_ID = "X-Optum-Tenant-Id";

	public static final String ACCOUNT_SHORT_CODE = "accountShortCode";

	public static final String SUB_ACCOUNT_SHORT_CODE = "subAccountShortCode";

	public static final String BALANCES = "/balances";

	public static final String REQUIRED = "Required";

	public static final String SERVICE_SCOPE_KEY = "SERVICE";

	public static final int CHUNK_SIZE_FOR_RE_PROCESS_PAYMENT = 1000;

	public static final String INVALID_JSON_FORMAT_LOG = "Request body is malformed or not valid JSON";

	public static final String FIELD_TYPE_MISMATCH_LOG = "Field has invalid format or type";

	// Card Creation Constants
	public static final String CARD_CREATION_NOT_FOUND = "Card creation details not found for paymentId: ";

	public static final String CARD_CREATION_NOT_FOUND_IN_TABLE = "Card creation details not found for paymentId: %s in cardCreation table";

	public static final String EMPTY_RESPONSE_FROM_CARD_API = "Empty response body received from card creation status API for paymentId: ";

	public static final String CARD_CREATION_STATUS_NOT_FOUND = "Card Creation Status not found for paymentId: ";

	// Card History Constants
	public static final String CARD_HISTORY_NOT_FOUND = "No card history found for cardGuid: ";

	public static final String INVALID_UUID_FORMAT = "Invalid UUID format for cardGuid: ";

	public static final String ERROR_FETCHING_CARD_HISTORY = "Error while fetching virtual card history details for cardGuid: ";

	public static final String STP_REJECTED = "STP_REJECTED";

	public static final String RECEIVED = "RECEIVED";

	public static final String ERRORCODE = "ERR01";

	public static final String MESSAGE = "Needed by ops";

	public static final String EVENT_STATUS = "FAILED";

	public static final String EVENT_TYPE = "virtualcard.create";

	public static final String SPEC_VERSION = "1.0";

	public static final String OTHER = "OTHER";

	private ApiConstants() {
	}

}
