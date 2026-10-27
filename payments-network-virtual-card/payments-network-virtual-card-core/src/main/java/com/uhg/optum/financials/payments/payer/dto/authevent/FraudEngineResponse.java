package com.uhg.optum.financials.payments.payer.dto.authevent;

import lombok.Data;
import lombok.Getter;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
@Data
public class FraudEngineResponse {

	private String advice;

	private List<String> triggeredRules;

	private List<String> ruleTags;

}
