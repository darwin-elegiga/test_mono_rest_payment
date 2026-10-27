package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.payments.payer.constant.RuleType;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.SequenceGenerator;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

import java.time.LocalDateTime;
import java.util.List;

@Entity
@Getter
@Setter
@Table(name = "manage_spend_rules", schema = "virtual_card")
public class ManageSpendRules {

	@Id
	@GeneratedValue(strategy = GenerationType.SEQUENCE, generator = "virtual_card.manage_spend_rules_id_seq")
	@SequenceGenerator(name = "virtual_card.manage_spend_rules_id_seq", allocationSize = 1)
	private Long id;

	@Column(name = "rule_id", unique = true, nullable = false)
	private String ruleId;

	@Column(name = "optum_rule_schema_id", nullable = false)
	private String optumRuleSchemaId;

	@Enumerated(EnumType.STRING)
	@Column(name = "rule_type", nullable = false)
	private RuleType ruleType;

	@Column(name = "rule_name", nullable = false)
	private String ruleName;

	@Column(name = "maximum_amount", nullable = false)
	private Long maximumAmount;

	@Column(name = "risk_score")
	private Integer riskScore;

	@Column(name = "rule_allowed_mcc")
	private List<String> allowedMcc;

	@Column(name = "rule_disallowed_mcc")
	private List<String> disallowedMcc;

	@Column(name = "rule_allowed_merchant_ids")
	private List<String> allowedMerchantIds;

	@Column(name = "rule_disallowed_merchant_ids")
	private List<String> disallowedMerchantIds;

	@Column(name = "created_at", nullable = false)
	private LocalDateTime createdAt;

	@Column(name = "created_by", length = 40)
	private String createdBy;

	@Column(name = "updated_at")
	private LocalDateTime updatedAt;

	@Column(name = "updated_by", length = 40)
	private String updatedBy;

}
