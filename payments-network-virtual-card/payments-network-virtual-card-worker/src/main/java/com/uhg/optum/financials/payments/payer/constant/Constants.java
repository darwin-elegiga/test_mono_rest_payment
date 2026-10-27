package com.uhg.optum.financials.payments.payer.constant;

import java.util.regex.Pattern;

public final class Constants {

	private Constants() {
	}

	public static final String LOADING = "loading";

	public static final String DOT_DELIMITER = ".";

	public static final String COMPLETED = "completed";

	public static final String ISSUED = "issued";

	public static final String FAILED = "failed";

	public static final String REVERSED = "reversed";

	public static final String REVERSE = "reverse";

	public static final String AUTH = "auth";

	public static final String PAYMENT = "payment";

	public static final String VOID = "void";

	public static final String VOIDED = "voided";

	public static final String APPROVED = "Approved";

	public static final String DECLINED = "declined";

	public static final String FAILED_EVENT = "FAILED";

	public static final String SUCCESS_EVENT = "SUCCESS";

	public static final String SETTLED = "settled";

	public static final String CREATE = "create";

	public static final String CANCEL = "cancel";

	public static final String RESET = "reset";

	public static final String IMAGE_TYPE_FORMAT = "png";

	public static final String IMAGE_TYPE = ".png";

	public static final String SPACE = " ";

	public static final String BARCODE_IMAGE_START_NAME = "/barcode_";

	public static final String REFUND = "refund";

	public static final String VIRTUAL_CARD_NON_CDE_SERVICE = "VirtualCardNONCDEService";

	public static final Pattern NEW_LINE_ONLY = Pattern.compile("\n");

}
