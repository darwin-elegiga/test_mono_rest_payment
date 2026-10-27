package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Data;

import java.time.LocalDateTime;

@Entity
@Table(name = "VIRTUAL_CARD_DOCUMENT_CUSTOM_REMARKS", schema = "virtual_card")
@Data
public class DocumentCustomRemarks {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Integer id;

	@Column(name = "org_short_code", length = 20)
	private String orgShortCode;

	@Column(name = "account_short_code", length = 20)
	private String accountShortCode;

	@Column(name = "sub_account_short_code", length = 20)
	private String subAccountShortCode;

	@Column(name = "custom_remarks1")
	private String customRemarks1;

	@Column(name = "custom_remarks2")
	private String customRemarks2;

	@Column(name = "created_at", nullable = false, updatable = false)
	private LocalDateTime createdAt;

}
