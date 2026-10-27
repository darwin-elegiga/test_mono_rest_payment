package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class SystemAttributes {

	private String associatedAt;

	private String cardStatusModifiedAt;

	private String incorrectPinCount;

	private String incorrectPinDailyCount;

}
