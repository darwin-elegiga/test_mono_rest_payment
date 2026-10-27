package com.uhg.optum.financials.payments.payer.constant;

public final class AppConstants {

	private AppConstants() {
	}

	public static final String ECC_PUBLIC_KEY_KEY = "ecc-encryption-public-key";

	public static final String ECC_PRIVATE_KEY_KEY = "ecc-encryption-private-key";

	public static final String INFO = "info";

	public static final String ERROR = "error";

	public static final String DEBUG = "debug";

	public static final String ERROR_PRIORITY = "errorPriority";

	public static final String NAME = "name";

	public static final String STATUS = "status";

	public static final String VIRTUAL_CARD = "VIRTUAL_CARD";

	public static final String VIRTUALCARD = "VIRTUALCARD";

	public static final String RS1_0001 = "RS1-0001";

	public static final String RS2_0004 = "RS2-0004";

	public static final int BIN_654231 = 654231;

	public static final String CARD_CREATION_STATUS_MESSAGE = "Card creation status set to: ";

	public static final String CARD_CANCELLATION_STATUS_MESSAGE = "Card cancellation status set to: ";

	public static final String CARD_RESET_STATUS_MESSAGE = "Card reset status set to: ";

	public static final String INVOKING_WEBHOOK_ENDPOINT = "Invoking webhook endpoint: ";

	public static final String WITH_TENANT_ID = " with tenantId: ";

	public static final String VIRTUAL_CARD_CREATION_REQUEST_FAILED = "VIRTUAL_CARD_CREATION_REQUEST_FAILED";

	public static final String CARD_ISSUE_FAILED = "CARD_ISSUE_FAILED";

	public static final String CARD_CANCELLATION_FAILED = "CARD_CANCELLATION_FAILED";

	public static final String AUTH_CLEAR_FAILED = "AUTH_CLEAR_FAILED";

	public static final String UNKNOWN_CARD = "Unknown Card";

	public static final String VOID_FAILED = "VOID_FAILED";

	public static final String VOIDED = "voided";

	public static final String ZETA_JID = "zeta-jid";

	public static final String ZETA_CLIENT_ID = "zeta-client-id";

	public static final String ZETA_CLIENT_SECRET = "zeta-client-secret";

	public static final String ZETA_SETUP_PRIVATE_KEY = "zeta-setup-private-key";

	public static final String ZETA_SETUP_PUBLIC_KEY = "zeta-setup-public-key";

	public static final String TLS_PREMASTER_SECRET = "TlsPremasterSecret";

	public static final String ECDH = "ECDH";

	public static final String EC = "EC";

	public static final String SECP256R1 = "secp256r1";

	public static final String VIRTUAL_CARD_CANCELLATION_REQUEST_FAILED = "VIRTUAL_CARD_CANCELLATION_REQUEST_FAILED";

	public static final String CORRELATION_EVENT_EXCEPTION_LOG = "Exception occurred while sending correlation event";

	public static final String CORRELATION_MSG_EXCEPTION_LOG = "Exception while sending the correlation message to the queue: ";

	public static final String UNEXPECTED_RESPONSE_STATUS = "unexpected response status";

	public static final String CANCELLATION_ERROR_MSG = "Error during card cancellation:";

	public static String AUTH_FILE_REGEX = "^Card_Authorisation_Extract_\\d{2}-[A-Za-z]{3}-\\d{4}_\\d{2}_\\d{2}_\\d{2}\\.csv$";

	public static String CLEAR_FILE_REGEX = "^Clearance_Extract_\\d{2}-[A-Za-z]{3}-\\d{4}_\\d{2}_\\d{2}_\\d{2}\\.csv$";

	public static String INVALID_CARD_DECLINE_FILE_REGEX = "^Invalid_Card_Decline_Auth_Extract_\\d{2}-[A-Za-z]{3}-\\d{4}_\\d{2}_\\d{2}_\\d{2}\\.csv$";

	public static final String PAYMENT_MSG_EXCEPTION_LOG = "Exception while sending the payment message to the queue: ";

	public static final String CREATE_CARD_MSG_EXCEPTION_LOG = "Exception while sending the create virtual card message to the queue :,";

	public static final String URL = "https://example.com/document.pdf";

	public static final String CARD = "card";

	public static final String ACTIVE = "Active";

	public static final String IN_ACTIVE = "Inactive";

	public static final String DEBIT = "Debit";

	public static final String FOR = "for:";

	public static final String CLEAR_FILE_EXTRACT_PROCESSOR = "CLEAR_FILE_EXTRACT_PROCESSOR";

	public static final String AUTH_FILE_EXTRACT_PROCESSOR = "AUTH_FILE_EXTRACT_PROCESSOR";

	public static final String INVALID_CARD_DECLINE_AUTH_EXTRACT_PROCESSOR = "INVALID_CARD_DECLINE_EXTRACT_PROCESSOR";

	public static final String CLEAR = "CLEAR";

	public static final String AUTH = "AUTH";

	public static final String INVALID_CARD_AUTH_DECLINE = "INVALID_CARD_AUTH_DECLINE";

	public static final String AUTH_RESET = "AUTH_RESET";

	public static final String VIRTUAL_CARD_RESET_REQUEST_FAILED = "VIRTUAL_CARD_RESET_REQUEST_FAILED";

	public static final String RESET_ERROR_MSG = "Error during reset card:";

	public static final String CARD_GUID = "{cardGuid}";

	public static final String CREDIT = "Credit";

	public static final String UNABLE_TO_RESET = "Unable to Reset";

	public static final String CARD_AUTH_REVERSED = "card.auth.reversed";

	public static final String CARD_AUTH_REVERSED_FAILED = "card.auth.reversed.failed";

	public static final String VIRTUAL_CARD_NOT_FOUND_ERROR = "VirtualCard not found for cardGuid:";

	public static final String PAYMENT_DETAILS_NOT_FOUND_ERROR = "PaymentDetails not found for id:";

	public static final String CORRELATION_ID_REQUIRED_ERROR = "correlationId is required";

	public static final String PAYMENT_REQUEST_NOT_FOUND_BY_CORRELATION_ID_ERROR = "No payment requests found for correlationId: ";

	public static final String BATCH_DETAIL_NOT_FOUND_BY_CORRELATION_ID_ERROR = "No batch details found for correlationId: ";

	public static final String NO_VALID_RECORDS_MSG_TEMPLATE = "File is Empty or No valid records found in the %s file: %s";

	public static final String ERROR_MESSAGE = "with error:";

	public static final String BATCH_DETAIL_STATUS_INITIALIZE = "P";

	public static final String BATCH_DETAIL_STATUS_835_CREATED = "R";

	public static final String BATCH_DETAIL_STATUS_COMPANION_CREATED = "C";

	public static final String BATCH_DETAIL_STATUS_FAILED = "F";

	public static final String BATCH_DETAIL_STATUS_PROGRESS = "I";

	public static final String FUNDING_FILE = "835FINSEG";

	public static final String OPTUM = "OPTUM";

	public static final String OPTUMPAY_NETWORK = "OPTUMPAY";

	public static final String TOKEN_CACHE_NAME = "tokenCache";

	public static final String TOKEN_CACHE_KEY = "'zetaToken'";

	public static final String ST_SE_EX = "ST*835*ST_INDEX_PLACEHOLDER~";

	public static final boolean DEFAULT_HAS_CUSTOM_LOGO = false;

	public static final String CARD_AUTH_EVENT_APPROVED = "card.auth.approved";

	public static final String CARD_AUTH_EVENT_DECLINED = "card.auth.declined";

	public static final String ERROR_CONVERTING_DTO_TO_JSON = "Error converting DTO to JSON: ";

	public static final String OTHER = "OTHER";

	public static final String ZETA_ECC_PUBLIC_KEY_KEY = "zeta-ecc-encryption-public-key";

	public static final String ZETA_ECC_PRIVATE_KEY_KEY = "zeta-ecc-encryption-private-key";

	public static final String AUTH_CLEAR = "AUTH_CLEAR";

	public static final String AUTH_RESET_CLEAR = "AUTH_RESET_CLEAR";

	public static final String IN_PROGRESS = "IN_PROGRESS";

	public static final String AUTH_RESET_FAILED = "AUTH_RESET_FAILED";

	public static final String PAYMENT_METHOD = "VCP";

	public static final int RE_PREFERENCE_MAX_ATTEMPTS = 3;

	public static final String PROCESSED = "PROCESSED";

	public static final String CALLBACK_PROCESSING_FAILED = "CALLBACK_PROCESSING_FAILED";

}
