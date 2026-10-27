package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Table;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Column;
import jakarta.validation.constraints.NotNull;
import lombok.Data;
import org.hibernate.annotations.CreationTimestamp;

import java.time.LocalDateTime;
import java.util.UUID;

@Entity
@Data
@Table(name = "company_logo_details", schema = "virtual_card")
public class CompanyLogoDetails {

	@Id
	@GeneratedValue(strategy = GenerationType.UUID)
	@Column(name = "id")
	private UUID logoId;

	@NotNull
	@Column(name = "logo_url")
	private String logoUrl;

	@NotNull
	@Column(name = "type")
	private String type;

	@Column(length = 40)
	private String createdById;

	/**
	 * nullable persistent field.
	 */
	@CreationTimestamp
	private LocalDateTime createdOn;

	/**
	 * persistent field.
	 */
	@Column(length = 40)
	private String updatedById;

	/**
	 * nullable persistent field.
	 */
	private LocalDateTime lastUpdate;

}
