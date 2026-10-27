package com.uhg.optum.financials.payments.payer.constant;

public final class PaymentDistributionConstants {

	private PaymentDistributionConstants() {
	}

	// ===== CONFIGURATION CONSTANTS =====
	public static final String HTML_EXTENSION = ".html";

	public static final String HTML_ZIP_FOLDER = "html/";

	public static final String LOGOS_ZIP_FOLDER = "logos/";

	public static final String PAYMENT_PREFIX = "PAYMENT_";

	public static final String DEFAULT_DISTRIBUTION_METHOD = "EMAIL";

	public static final String SUPPLEMENTAL_HEADER = "paymentId|paymentDate|paymentAmount|cguid|trackingNumber|docName|logo|distributionMethod|mailTo|mailAddressline1|mailAddressline2|mailCity|mailState|mailPostalCode|mailCountry|returnName|returnAddressline1|returnAddressline2|returnCity|returnState|returnPostalCode|mailMethod|faxPhoneNumber|faxHeaderName";

	// ===== LOGO RESOURCE PATHS =====
	public static final String OPTUM_LOGO_RESOURCE = "images/optumLogo.png";

	public static final String MASTERCARD_LOGO_RESOURCE = "images/masterCard.png";

	public static final String OPTUM_LOGO_ZIP_ENTRY = "logos/optum_logo.png";

	public static final String MASTERCARD_LOGO_ZIP_ENTRY = "logos/mastercard_logo.png";

	// ===== MAIL ADDRESS FIELD CONSTANTS =====
	public static final String FIELD_MAIL_ADDRESS_LINE1 = "mailAddressline1";

	public static final String FIELD_MAIL_ADDRESS_LINE2 = "mailAddressline2";

	public static final String FIELD_MAIL_CITY = "mailCity";

	public static final String FIELD_MAIL_STATE = "mailState";

	public static final String FIELD_MAIL_POSTAL_CODE = "mailPostalCode";

	public static final String FIELD_MAIL_COUNTRY = "mailCountry";

	// ===== RETURN ADDRESS FIELD CONSTANTS =====
	public static final String FIELD_RETURN_NAME = "returnName";

	public static final String FIELD_RETURN_ADDRESS_LINE1 = "returnAddressline1";

	public static final String FIELD_RETURN_ADDRESS_LINE2 = "returnAddressline2";

	public static final String FIELD_RETURN_CITY = "returnCity";

	public static final String FIELD_RETURN_STATE = "returnState";

	public static final String FIELD_RETURN_POSTAL_CODE = "returnPostalCode";

	// ===== DISTRIBUTION METHOD CONSTANTS =====
	public static final String DISTRIBUTION_METHOD_PRINT = "PRINT";

	public static final String DISTRIBUTION_METHOD_FAX = "FAX";

	// ===== DATE FORMAT CONSTANTS =====
	public static final String DATE_FORMAT_TIMESTAMP = "yyyyMMddHHmmss";

	// ===== LOGO NAME CONSTANTS =====
	public static final String LOGO_NAME_OPTUM = "Optum";

	public static final String LOGO_NAME_MASTERCARD = "Mastercard";

	// ===== IMAGE FILE EXTENSION CONSTANTS =====
	public static final String IMAGE_EXT_JPG = ".jpg";

	public static final String IMAGE_EXT_JPEG = ".jpeg";

	public static final String IMAGE_EXT_GIF = ".gif";

	public static final String IMAGE_EXT_PNG = ".png";

	public static final String IMAGE_TYPE_GIF = "gif";

	public static final String IMAGE_TYPE_PNG = "png";

	public static final String IMAGE_TYPE_JPEG = "jpeg";

	// ===== CARD AND PAYMENT TYPE CONSTANTS =====
	public static final String CARD_PAN = "PAN";

	public static final String PAYMENT_TYPE_VIRTUAL_CARD = "VIRTUAL_CARD";

	// ===== BATCH STATUS CONSTANTS =====
	public static final String BATCH_STATUS_PENDING = "PENDING"; // Batch event received.
																	// HTML generation not
																	// yet started.

	public static final String BATCH_STATUS_ZIP_GENERATED = "ZIP_GENERATED"; // All HTML
																				// files
																				// generated
																				// and
																				// uploaded.
																				// ZIP
																				// built
																				// and
																				// uploaded.
																				// Transmission
																				// to Zeta
																				// in
																				// progress.

	public static final String BATCH_STATUS_SENT = "SENT"; // ZIP successfully sent to
															// Blob. Terminal success
															// state.

	public static final String BATCH_STATUS_FAILED = "FAILED"; // ZIP transmission failed
																// after max retries.
																// Terminal failure.
																// Requires manual
																// intervention.

	// ===== RECORD STATUS CONSTANTS =====
	public static final String RECORD_STATUS_PENDING = "PENDING"; // Record row created.
																	// HTML generation not
																	// yet started.

	public static final String RECORD_STATUS_HTML_CREATED = "HTML_CREATED"; // HTML
																			// generated
																			// and
																			// successfully
																			// uploaded to
																			// blob
																			// storage.

	public static final String RECORD_STATUS_SUCCESS = "SUCCESS"; // Zeta acknowledged
																	// this record as
																	// successfully
																	// processed. Terminal
																	// success.

	public static final String RECORD_STATUS_FAILED = "FAILED";

	public static final String BATCH_STATUS_CREATED = "CREATED";

	// Zeta reported this record as failed. Stays FAILED permanently. New record created
	// in retry batch.

	// ===== FILE EXTENSION CONSTANTS =====
	public static final String FILE_EXT_ZIP = ".zip";

	// ===== LOG MESSAGE CONSTANTS =====
	public static final String LOG_MISSING_FIELD_TEMPLATE = "Missing {} field: {} for {} distribution, payment ID: {}";

	public static final String LOG_MISSING_FIELD_GENERIC = "Missing {} field: {} for payment ID: {}";

	// Log field requirement levels
	public static final String LOG_FIELD_REQUIRED = "required";

	public static final String LOG_FIELD_RECOMMENDED = "recommended";

	// Log distribution contexts
	public static final String LOG_DISTRIBUTION_PRINT = "PRINT";

	public static final String LOG_DISTRIBUTION_FAX = "FAX";

	// ===== BLOB STORAGE PATH CONSTANTS =====
	public static final String BLOB_STORAGE_TMP_FOLDER = "tmp";

	public static final String BLOB_STORAGE_DISTRIBUTION_FOLDER = "distribution";

}
