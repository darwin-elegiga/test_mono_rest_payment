package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.PrePersist;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.UpdateTimestamp;

import java.time.LocalDateTime;

@Entity
@Getter
@Setter
@Table(name = "consent_required_states", schema = "virtual_card")
public class ConsentRequiredState {

	@Id
	@Column(name = "state_code", nullable = false, length = 2)
	private String stateCode;

	@Column(name = "display_name", nullable = false, length = 100)
	private String displayName;

	@Column(name = "country_code", nullable = false, length = 2)
	private String countryCode;

	@Column(name = "consent_required", nullable = false)
	private Boolean consentRequired;

	@Column(name = "is_active", nullable = false)
	private Boolean isActive;

	@CreationTimestamp
	@Column(name = "created_at", nullable = false, updatable = false)
	private LocalDateTime createdAt;

	@Column(name = "created_by", nullable = false, length = 100)
	private String createdBy;

	@UpdateTimestamp
	@Column(name = "updated_at", nullable = false)
	private LocalDateTime updatedAt;

	@Column(name = "updated_by", nullable = false, length = 100)
	private String updatedBy;

	@PrePersist
	public void prePersist() {
		if (this.countryCode == null) {
			this.countryCode = "US";
		}
		if (this.consentRequired == null) {
			this.consentRequired = Boolean.FALSE;
		}
		if (this.isActive == null) {
			this.isActive = Boolean.TRUE;
		}
	}

}
