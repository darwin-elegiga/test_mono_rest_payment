package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Data;

import java.time.LocalDateTime;
import java.util.List;

@Data
@Entity
@Table(name = "spend_rules", schema = "virtual_card")
public class SpendRules {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Long id;

	@Column(name = "rule_code", nullable = false)
	private List<String> ruleCode;

	@Column(name = "org_short_code", nullable = false, length = 20)
	private String orgShortCode;

	@Column(name = "account_short_code", length = 20)
	private String accountShortCode;

	@Column(name = "sub_account_short_code", length = 20)
	private String subAccountShortCode;

	@Column(name = "org_name", nullable = false, length = 250)
	private String orgName;

	@Column(name = "account_name", length = 250)
	private String accountName;

	@Column(name = "sub_account_name", length = 250)
	private String subAccountName;

	@Column(name = "created_at", nullable = false)
	private LocalDateTime createdAt;

	@Column(name = "created_by", length = 40)
	private String createdBy;

	@Column(name = "updated_at")
	private LocalDateTime updatedAt;

	@Column(name = "updated_by", length = 40)
	private String updatedBy;

	public SpendRules() {
		this.createdAt = LocalDateTime.now();
	}

}
