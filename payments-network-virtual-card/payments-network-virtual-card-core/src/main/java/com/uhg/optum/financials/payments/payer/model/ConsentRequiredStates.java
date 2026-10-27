package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

import java.time.LocalDateTime;

@Entity
@Getter
@Setter
@Table(name = "consent_required_states", schema = "virtual_card")
public class ConsentRequiredStates {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	@Column(name = "id")
	private Long id;

	@Column(name = "state_code", length = 2, nullable = false)
	private String stateCode;

	@Column(name = "state_name", length = 100, nullable = false)
	private String stateName;

	@Column(name = "consent_required", nullable = false)
	private boolean consentRequired;

	@Column(name = "created_at", nullable = false, columnDefinition = "TIMESTAMP DEFAULT CURRENT_TIMESTAMP")
	private LocalDateTime createdAt;

	@Column(name = "created_by", nullable = false, length = 100)
	private String createdBy;

	@Column(name = "updated_at", nullable = false, columnDefinition = "TIMESTAMP DEFAULT CURRENT_TIMESTAMP")
	private LocalDateTime updatedAt;

	@Column(name = "updated_by", nullable = false, length = 100)
	private String updatedBy;

}
