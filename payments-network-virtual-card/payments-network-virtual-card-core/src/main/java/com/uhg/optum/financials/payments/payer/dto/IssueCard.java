package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
public class IssueCard {

	private String requestID; // Mandatory

	private String formFactorProductID; // Mandatory

	private String targetAccountID; // Mandatory

	private String phoneNumber; // Optional

	private Attributes attributes; // Mandatory

	private OrderDetails orderDetails; // Mandatory

	private List<String> tags; // Optional

	private List<String> vectors; // Optional

}
