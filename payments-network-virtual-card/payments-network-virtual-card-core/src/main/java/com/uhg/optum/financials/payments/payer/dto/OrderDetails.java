package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

import java.util.Map;

@Getter
@Setter
public class OrderDetails {

	private String orderID;

	private String cardSkuID;

	private CardSku cardSku; // Mandatory

	private String plasticCode;

	private String thirdLineEmbossing;

	private String fourthLineEmbossing;

	private Expiry expiry;

	private DeliveryAddress deliveryAddress;

	private Map<String, Object> tenantAttributes;

	private Map<String, Object> vendorAttributes;

	private Map<String, Object> additionalAttributes;

	private String orderStatus;

	private Map<String, FileDetails> files;

	private String issuanceType;

}
