package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class DeliveryAddress {

	private String name;

	private String addressLine1;

	private String addressLine2;

	private String addressLine3;

	private String addressLine4;

	private String city;

	private String state;

	private String country;

	private String postalCode;

	private String contactNumber;

}
