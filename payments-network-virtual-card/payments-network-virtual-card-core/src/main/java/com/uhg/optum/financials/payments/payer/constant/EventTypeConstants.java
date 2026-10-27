package com.uhg.optum.financials.payments.payer.constant;

public final class EventTypeConstants {

	private EventTypeConstants() {
	}

	public static final String VIRTUAL_CARD_CREATE = "virtualcard.create";

	public static final String VIRTUAL_CARD_CANCEL = "virtualcard.cancel";

	public static final String VIRTUAL_CARD_RESET = "virtualcard.reset";

	public static final String AUTH_EVENT = "virtualcard.auth";

	public static final String AUTH_EVENT_APPROVED = "APPROVED";

	public static final String AUTH_EVENT_DECLINED = "DECLINED";

	public static final String AUTH_PAYMENT_EFFECTED = "PAYMENT_EFFECTED";

}
