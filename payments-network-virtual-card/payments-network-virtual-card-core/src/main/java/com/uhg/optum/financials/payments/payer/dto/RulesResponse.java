package com.uhg.optum.financials.payments.payer.dto;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class RulesResponse {

	private List<RuleViewResponse> data;

	private PageMetadata page;

	@Getter
	@Setter
	@AllArgsConstructor
	@NoArgsConstructor
	public static class PageMetadata {

		private Integer size;

		private Integer number;

		private Integer totalElements;

		private Integer totalPages;

	}

	@Getter
	@Setter
	@AllArgsConstructor
	@NoArgsConstructor
	public static class RuleViewResponse {

		private String ruleID;

		private String ruleType;

		private String name;

		private String optumRuleSchemaId;

		private Long maximumAmountUSD;

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

}
