package com.uhg.optum.financials.payments.payer.dto;

import com.uhg.optum.financials.payments.payer.constant.RuleType;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import lombok.Data;

import java.util.List;

@Data
public class RuleDto {

	@NotBlank(message = "ruleId is mandatory")
	private String ruleId;

	private String optumRuleSchemaId;

	@NotNull(message = "ruleType is mandatory. Allowed values: "
			+ com.uhg.optum.financials.payments.payer.constant.RuleType.ALLOWED_VALUES)
	private RuleType ruleType;

	@NotBlank(message = "Rule name is required")
	@Size(max = 255)
	private String ruleName;

	@NotNull(message = "Maximum amount (USD in cents) is required")
	private Long maximumAmount;

	@Min(value = 1, message = "Risk Score (1-100) must be at least 1")
	@Max(value = 100, message = "Risk Score (1-100) must not exceed 100")
	private Integer riskScore;

	private List<String> allowedMcc;

	private List<String> disallowedMcc;

	private List<String> allowedMerchantIds;

	private List<String> disallowedMerchantIds;

}
