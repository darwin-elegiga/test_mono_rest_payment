package com.uhg.optum.financials.payments.payer.model.events;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

import java.time.Instant;
import java.time.LocalDateTime;
import java.util.UUID;

@Entity
@Getter
@Setter
@Table(name = "organizations", schema = "virtual_card")
public class Organization {

	@Id
	@Column(name = "org_id")
	private UUID orgId;

	@Column(name = "market_type")
	private String marketType;

	@Column(name = "org_status")
	private String orgStatus;

	@Column(name = "name")
	private String name;

	@Column(name = "fax_number")
	private String faxNumber;

	@Column(name = "phone_number")
	private String phoneNumber;

	@Column(name = "contact_name")
	private String contactName;

	@Column(name = "contact_email")
	private String contactEmail;

	@Column(name = "tin")
	private String tin;

	@Column(name = "program_id")
	private String programId;

	@Column(name = "funding_method")
	private String fundingMethod;

	@Column(name = "claim_office_phone_number")
	private String claimOfficePhoneNumber;

	@Column(name = "claim_office_phone_number_extension")
	private String claimOfficePhoneNumberExtension;

	@Column(name = "claim_office_url")
	private String claimOfficeUrl;

	@Column(name = "technical_contact_name")
	private String technicalContactName;

	@Column(name = "technical_phone_number")
	private String technicalPhoneNumber;

	@Column(name = "technical_phone_number_extension")
	private String technicalPhoneNumberExtension;

	@Column(name = "technical_url")
	private String technicalUrl;

	@Column(name = "technical_fax_number")
	private String technicalFaxNumber;

	@Column(name = "technical_email_address")
	private String technicalEmailAddress;

	@Column(name = "onboarded_on_dt")
	private Instant onBoardedOn;

	@Column(name = "created_on")
	private LocalDateTime createdOn;

	@Column(name = "updated_on")
	private LocalDateTime updatedOn;

	@Column(name = "created_by")
	private String createdBy;

	@Column(name = "updated_by")
	private String updatedBy;

	@Column(name = "onboarding_status")
	private String onboardingStatus;

	@Column(name = "has_custom_logo")
	private Boolean hasCustomLogo;

	@Setter
	@Getter
	// @OneToOne(cascade = CascadeType.ALL)
	@Column(name = "address_id")
	private Long addressId;

	@Setter
	@Getter
	@Column(name = "org_short_code")
	private String orgShortCode;

	@Column(name = "type")
	private String type;

}
