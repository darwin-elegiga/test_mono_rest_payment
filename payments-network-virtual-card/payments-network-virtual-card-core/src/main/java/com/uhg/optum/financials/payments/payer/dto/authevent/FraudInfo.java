package com.uhg.optum.financials.payments.payer.dto.authevent;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class FraudInfo {

	private String fraudEngineAdvice;

	private String fraudEngineResponseType;

	private FraudEngineResponse fraudEngineResponse;

}
