package com.uhg.optum.financials.payments.payer.dto;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import java.util.List;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class RuleResponse {

	private String ruleID;

	private String name;

	private String ruleType;

	private Double maximumAmountUSD;

	private Integer riskScore;

	private List<String> allowedMCCs;

	private List<String> disallowedMCCs;

	private List<String> allowedMIDs;

	private List<String> disallowedMIDs;

	private String createdAt;

	private String updatedAt;

	private String createdBy;

	private String updatedBy;

}
