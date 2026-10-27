package com.uhg.optum.financials.payments.payer.model;

import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

@Entity
@Getter
@Setter
@Table(name = "payment_grouping_level", schema = "virtual_card")
public class PaymentGroupingLevel extends BaseEntity {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Long id;

	@Column(name = "org_short_code")
	@Schema(description = "org short code", example = "OXFD5")
	private String orgShortCode;

	@Column(name = "group_level")
	@Schema(description = "group level", example = "ORG")
	private String groupLevel;

}
