package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.payments.organization.enums.MarketType;
import com.uhg.optum.financials.payments.organization.events.AccountRegistered;
import com.uhg.optum.financials.payments.payer.dto.account.AccountDto;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

import java.util.Optional;
import java.util.UUID;

@Entity
@Getter
@Setter
@Table(name = "account", schema = "virtual_card")
public class Account extends BaseOrganizationEventsDetails {

	@Id
	private UUID accountId;

	@Column(name = "account_short_code")
	private String accountShortCode;

	@Column(name = "onboarding_status")
	private String onboardingStatus;

	public AccountDto toDto(AccountDto accountDto) {
		accountDto = Optional.ofNullable(accountDto).orElse(new AccountDto());

		AccountRegistered accountRegistered = new AccountRegistered();
		accountRegistered.setAccountId(this.accountId);
		accountRegistered.setTin(this.getTin());
		accountRegistered.setMarketType(MarketType.valueOf(this.getMarketType()));
		accountRegistered.setName(this.getName());
		accountRegistered.setFaxNumber(this.getFaxNumber());
		accountRegistered.setPhoneNumber(this.getPhoneNumber());
		accountRegistered.setContactEmail(this.getContactEmail());
		accountRegistered.setHoldAndRelease(this.getHoldAndRelease());
		accountRegistered.setProgramId(this.getProgramId());
		accountRegistered.setPayer835Id(this.getPayer835Id());
		accountRegistered.setClaimOfficePhoneNumber(this.getClaimOfficePhoneNumber());
		accountRegistered.setClaimOfficePhoneNumberExtension(this.getClaimOfficePhoneNumberExtension());
		accountRegistered.setClaimOfficeUrl(this.getClaimOfficeURL());
		accountRegistered.setTechnicalContactName(this.getTechnicalContactName());
		accountRegistered.setTechnicalPhoneNumber(this.getTechnicalPhoneNumber());
		accountRegistered.setTechnicalPhoneNumberExtension(this.getTechnicalPhoneNumberExtension());
		accountRegistered.setTechnicalUrl(this.getTechnicalURL());
		accountRegistered.setTechnicalFaxNumber(this.getTechnicalFaxNumber());
		accountRegistered.setTechnicalEmailAddress(this.getTechnicalEmailAddress());
		accountRegistered.setAddress(this.toDto(accountRegistered.getAddress()));
		accountRegistered.setType(this.getType());
		accountRegistered.setShortCode(this.getAccountShortCode());
		accountRegistered.setContactName(this.getContactName());
		accountRegistered.setCreatedBy(this.getCreatedById());
		accountDto.setAccountRegistered(accountRegistered);
		return accountDto;
	}

}
