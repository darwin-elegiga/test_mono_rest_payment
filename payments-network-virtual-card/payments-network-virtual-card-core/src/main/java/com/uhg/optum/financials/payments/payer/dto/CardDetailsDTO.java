package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

import java.util.List;
import java.util.Map;

@Getter
@Setter
public class CardDetailsDTO {

	private String cardID;

	private String cardHolderID;

	private String crn;

	private String cardType;

	private String maskedPan;

	private String cardStatus;

	private StatusReason statusReason;

	private OrderDetails orderDetails;

	private List<String> vectors;

	private Map<String, String> tenantAttributes;

	private BinRange binRange;

	private int tenantID;

	private String cguid;

	private List<String> tags;

	private String validFromMMYY;

	private String expiryMMYY;

	private SystemAttributes systemAttributes;

	private String cardLifecycleID;

	private String issuanceType;

	private Map<String, Object> additionalAttributes;

	private BaseCardInfo baseCardInfo;

}
