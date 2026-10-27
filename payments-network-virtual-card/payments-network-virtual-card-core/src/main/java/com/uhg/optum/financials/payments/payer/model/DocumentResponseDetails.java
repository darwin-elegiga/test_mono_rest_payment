package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Table;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Column;
import lombok.Data;

import java.time.LocalDateTime;

@Entity
@Data
@Table(name = "document_response_details", schema = "virtual_card")
public class DocumentResponseDetails {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	@Column(name = "doc_response_id")
	private Long docResponseId;

	@Column(name = "document_id")
	private String documentId;

	@Column(name = "document_url")
	private String documentUrl;

	@Column(name = "error_desc")
	private String errorDesc;

	@Column(name = "doc_type")
	private String docType;

	@Column(name = "meta_data")
	private String metaData;

	@Column(name = "created_dt")
	private LocalDateTime createdDt;

	@Column(name = "created_by")
	private String createdBy;

	@Column(name = "last_changed_dt")
	private LocalDateTime lastChangedDt;

	@Column(name = "last_changed_by")
	private String lastChangedBy;

	@Column(name = "network_payment_id")
	private String networkPaymentId;

}
