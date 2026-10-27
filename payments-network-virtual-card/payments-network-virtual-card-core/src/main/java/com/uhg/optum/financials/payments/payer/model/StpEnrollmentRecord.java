package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.OffsetDateTime;

@Entity
@Table(name = "stp_enrollment_record", schema = "virtual_card")
@Getter
@Setter
@NoArgsConstructor
public class StpEnrollmentRecord {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Long id;

	@Column(name = "stp_enrollment_batch_id", nullable = false)
	private Long stpEnrollmentBatchId;

	@Column(name = "provider_tin", nullable = false, length = 20)
	private String providerTin;

	@Column(name = "legal_entity_name", length = 255)
	private String legalEntityName;

	@Column(name = "doing_business_as", length = 255)
	private String doingBusinessAs;

	@Column(name = "npi", length = 20)
	private String npi;

	@Column(name = "provider_uuid", length = 50)
	private String providerUuid;

	@Column(name = "address1", length = 255)
	private String address1;

	@Column(name = "address2", length = 255)
	private String address2;

	@Column(name = "city", length = 100)
	private String city;

	@Column(name = "state", length = 10)
	private String state;

	@Column(name = "zip9", length = 20)
	private String zip9;

	@Column(name = "enrollment_type", nullable = false, length = 10)
	private String enrollmentType;

	@Column(name = "enrollment_eligibility_status", length = 10)
	private String enrollmentEligibilityStatus;

	@Column(name = "effective_date")
	private LocalDate effectiveDate;

	@Column(name = "initial_enrollment_date")
	private LocalDate initialEnrollmentDate;

	@Column(name = "last_updated_utc_date")
	private OffsetDateTime lastUpdatedUtcDate;

	@Column(name = "enrollment_status_cd", length = 10)
	private String enrollmentStatusCd;

	@Column(name = "enrollment_status_reason", length = 255)
	private String enrollmentStatusReason;

	@Column(name = "status", nullable = false, length = 50)
	private String status;

	@Column(name = "error_message", columnDefinition = "TEXT")
	private String errorMessage;

	@Column(name = "created_at", nullable = false, columnDefinition = "TIMESTAMP DEFAULT CURRENT_TIMESTAMP")
	private LocalDateTime createdAt;

	@Column(name = "updated_at", nullable = false, columnDefinition = "TIMESTAMP DEFAULT CURRENT_TIMESTAMP")
	private LocalDateTime updatedAt;

}
