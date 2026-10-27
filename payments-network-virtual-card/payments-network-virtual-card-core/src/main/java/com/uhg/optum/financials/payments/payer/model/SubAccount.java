package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.payments.organization.enums.MarketType;
import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto;
import jakarta.persistence.CascadeType;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.OneToOne;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

import java.util.Optional;
import java.util.UUID;

@Entity
@Getter
@Setter
@Table(name = "sub_accounts", schema = "virtual_card")
public class SubAccount extends BaseOrganizationEventsDetails {

	@Id
	private UUID subAccountId;

	@Column(name = "sub_account_short_code")
	private String subAccountShortCode;

	@Column(name = "account_short_code")
	private String accountShortCode;

	@Column(name = "funding_foreign_account")
	private UUID fundingForeignAccount;

	@Column(name = "funding_local_account")
	private UUID fundingLocalAccount;

	@Column(name = "unload_foreign_account")
	private UUID unloadForeignAccount;

	@Column(name = "unload_local_account")
	private UUID unloadLocalAccount;

	@Column(name = "pay_from_bank_account")
	private UUID payFromBankAccount;

	@Column(name = "onboarding_status")
	private String onboardingStatus;

	@Column(name = "expiry_days")
	private Integer expiryDays;

	@Column(name = "is_single_spend")
	private Boolean isSingleSpend; // consume value of this column from subaccount
									// registered event

	@Getter
	@Setter
	private UUID accountId;

	@OneToOne(cascade = CascadeType.ALL)
	@JoinColumn(name = "return_address_id")
	private Address returnMailingAddress;

	public SubAccountDto toDto(SubAccountDto subAccountDto) {
		subAccountDto = Optional.ofNullable(subAccountDto).orElse(new SubAccountDto());

		SubAccountRegistered subAccountRegistered = new SubAccountRegistered();
		subAccountRegistered.setAccountId(this.getAccountId());
		subAccountRegistered.setAccountShortCode(this.getAccountShortCode());
		subAccountRegistered.setSubAccountId(this.subAccountId);
		subAccountRegistered.setTin(this.getTin());
		subAccountRegistered.setMarketType(MarketType.valueOf(this.getMarketType()));
		subAccountRegistered.setName(this.getName());
		subAccountRegistered.setFaxNumber(this.getFaxNumber());
		subAccountRegistered.setPhoneNumber(this.getPhoneNumber());
		subAccountRegistered.setContactEmail(this.getContactEmail());
		subAccountRegistered.setHoldAndRelease(this.getHoldAndRelease());
		subAccountRegistered.setProgramId(this.getProgramId());
		subAccountRegistered.setPayer835Id(this.getPayer835Id());
		subAccountRegistered.setClaimOfficePhoneNumber(this.getClaimOfficePhoneNumber());
		subAccountRegistered.setClaimOfficePhoneNumberExtension(this.getClaimOfficePhoneNumberExtension());
		subAccountRegistered.setClaimOfficeUrl(this.getClaimOfficeURL());
		subAccountRegistered.setTechnicalContactName(this.getTechnicalContactName());
		subAccountRegistered.setTechnicalPhoneNumber(this.getTechnicalPhoneNumber());
		subAccountRegistered.setTechnicalPhoneNumberExtension(this.getTechnicalPhoneNumberExtension());
		subAccountRegistered.setTechnicalUrl(this.getTechnicalURL());
		subAccountRegistered.setTechnicalFaxNumber(this.getTechnicalFaxNumber());
		subAccountRegistered.setTechnicalEmailAddress(this.getTechnicalEmailAddress());
		subAccountRegistered.setAddress(this.toDto(subAccountRegistered.getAddress()));
		subAccountRegistered.setType(this.getType());
		subAccountRegistered.setShortCode(this.getSubAccountShortCode());
		subAccountRegistered.setFundingAccounts(this.toDto(subAccountRegistered.getFundingAccounts()));
		subAccountRegistered.setContactName(this.getContactName());
		subAccountRegistered.setCreatedBy(this.getCreatedById());
		subAccountRegistered.setReturnMailingAddress(this.toDto(subAccountRegistered.getReturnMailingAddress()));
		subAccountDto.setSubAccountRegistered(subAccountRegistered);
		return subAccountDto;
	}

	public com.uhg.optum.financials.payments.organization.models.FundingAccounts toDto(
			com.uhg.optum.financials.payments.organization.models.FundingAccounts fundingAccountsDto) {
		fundingAccountsDto = Optional.ofNullable(fundingAccountsDto)
			.orElse(new com.uhg.optum.financials.payments.organization.models.FundingAccounts());
		fundingAccountsDto.setFundingForeignAccount(this.fundingForeignAccount);
		fundingAccountsDto.setUnloadForeignAccount(this.unloadForeignAccount);
		fundingAccountsDto.setUnloadLocalAccount(this.unloadLocalAccount);
		fundingAccountsDto.setFundingLocalAccount(this.fundingLocalAccount);
		fundingAccountsDto.setPayFromBankAccount(this.payFromBankAccount);
		return fundingAccountsDto;
	}

	public void setReturnMailingAddress(Address returnMailingAddress) {
		this.returnMailingAddress = returnMailingAddress;
	}

	public void setReturnMailingAddress(com.uhg.optum.financials.payments.organization.models.Address dto) {
		Address addressObj = Optional.ofNullable(this.returnMailingAddress).orElse(new Address());
		addressObj.setLine1(dto.getAddressLine1());
		addressObj.setLine2(dto.getAddressLine2());
		addressObj.setCity(dto.getCity());
		addressObj.setState(dto.getState());
		addressObj.setCountry("us");
		addressObj.setPostalCode((dto.getPostalCode()));
		this.returnMailingAddress = addressObj;
	}

}
