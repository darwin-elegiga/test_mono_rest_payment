package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.CascadeType;
import jakarta.persistence.Column;
import jakarta.persistence.MappedSuperclass;
import jakarta.persistence.OneToOne;
import lombok.Getter;
import lombok.Setter;
import org.hibernate.annotations.CreationTimestamp;

import java.time.LocalDateTime;
import java.util.Optional;

@MappedSuperclass
@Getter
@Setter
public class BaseOrganizationEventsDetails {

	@Getter
	@Setter
	private String tin;

	@Getter
	@Setter
	@Column(name = "market_type")
	private String marketType;

	@Getter
	@Setter
	private String name;

	// Setter
	// @Getter
	@OneToOne(cascade = CascadeType.ALL)
	private Address address;

	@Getter
	@Setter
	private String faxNumber;

	@Getter
	@Setter
	private String phoneNumber;

	@Getter
	@Setter
	private String contactEmail;

	@Getter
	@Setter
	private Boolean holdAndRelease;

	@Getter
	@Setter
	@Column(name = "funding_method")
	private String fundingMethod;

	@Getter
	@Setter
	private String programId;

	@Getter
	@Setter
	@Column(name = "payer835_id")
	public String payer835Id;

	@Getter
	@Setter
	private String claimOfficePhoneNumber;

	@Getter
	@Setter
	private String claimOfficePhoneNumberExtension;

	@Getter
	@Setter
	private String claimOfficeURL;

	@Getter
	@Setter
	private String technicalContactName;

	@Getter
	@Setter
	private String technicalPhoneNumber;

	@Getter
	@Setter
	private String technicalPhoneNumberExtension;

	@Getter
	@Setter
	private String technicalURL;

	@Getter
	@Setter
	private String technicalFaxNumber;

	@Getter
	@Setter
	private String technicalEmailAddress;

	@Column(length = 40)
	@Getter
	@Setter
	private String createdById;

	/**
	 * nullable persistent field.
	 */
	@CreationTimestamp
	@Getter
	@Setter
	private LocalDateTime createdOn;

	/**
	 * persistent field.
	 */
	@Column(length = 40)
	@Getter
	@Setter
	private String updatedById;

	/**
	 * nullable persistent field.
	 */
	@Getter
	@Setter
	private LocalDateTime updatedOn;

	@Getter
	@Setter
	private String type;

	@Column(name = "org_short_code")
	private String orgShortCode;

	@Column(name = "has_custom_logo")
	private Boolean hasCustomLogo;

	@Column(name = "contact_name")
	private String contactName;

	public void setAddress(Address address) {
		this.address = address;
	}

	public void setAddress(com.uhg.optum.financials.payments.organization.models.Address dto) {
		Address addressObj = Optional.ofNullable(this.address).orElse(new Address());
		addressObj.setLine1(dto.getAddressLine1());
		addressObj.setLine2(dto.getAddressLine2());
		addressObj.setCity(dto.getCity());
		addressObj.setState(dto.getState());
		addressObj.setCountry("us");
		addressObj.setPostalCode((dto.getPostalCode()));
		this.address = addressObj;
	}

	public com.uhg.optum.financials.payments.organization.models.Address toDto(
			com.uhg.optum.financials.payments.organization.models.Address addressDto) {
		addressDto = Optional.ofNullable(addressDto)
			.orElse(new com.uhg.optum.financials.payments.organization.models.Address());
		addressDto.setAddressLine1(this.address.getLine1());
		addressDto.setAddressLine2(this.address.getLine2());
		addressDto.setCity(this.address.getCity());
		addressDto.setState(this.address.getState());
		addressDto.setPostalCode(String.valueOf(this.address.getPostalCode()));
		return addressDto;
	}

}
