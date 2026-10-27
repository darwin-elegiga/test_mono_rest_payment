package com.uhg.optum.financials.payments.payer.dto.account;

import com.uhg.optum.financials.payments.organization.events.AccountRegistered;
import com.uhg.optum.financials.payments.payer.model.Account;
import lombok.Getter;
import lombok.Setter;

import java.util.Optional;

@Getter
@Setter
public class AccountDto {

	@Getter
	@Setter
	AccountRegistered accountRegistered;

	public Account toEntity(Account account) {
		account = Optional.ofNullable(account).orElse(new Account());
		account.setMarketType(String.valueOf(this.accountRegistered.getMarketType()));
		account.setName(this.accountRegistered.getName());
		account.setFaxNumber(this.accountRegistered.getFaxNumber());
		account.setPhoneNumber(this.accountRegistered.getPhoneNumber());
		account.setContactEmail(this.accountRegistered.getContactEmail());
		account.setTin(this.accountRegistered.getTin());
		account.setHoldAndRelease(this.accountRegistered.isHoldAndRelease());
		account.setProgramId(this.accountRegistered.getProgramId());
		account.setPayer835Id(this.accountRegistered.getPayer835Id());
		account.setClaimOfficePhoneNumber(this.accountRegistered.getClaimOfficePhoneNumber());
		account.setClaimOfficePhoneNumberExtension(this.accountRegistered.getClaimOfficePhoneNumberExtension());
		account.setClaimOfficeURL(this.accountRegistered.getClaimOfficeUrl());
		account.setTechnicalContactName(this.accountRegistered.getTechnicalContactName());
		account.setTechnicalPhoneNumber(this.accountRegistered.getTechnicalPhoneNumber());
		account.setTechnicalPhoneNumberExtension(this.accountRegistered.getTechnicalPhoneNumberExtension());
		account.setTechnicalURL(this.accountRegistered.getTechnicalUrl());
		account.setTechnicalFaxNumber(this.accountRegistered.getTechnicalFaxNumber());
		account.setTechnicalEmailAddress(this.accountRegistered.getTechnicalEmailAddress());
		account.setAddress(this.accountRegistered.getAddress());
		account.setAccountId(this.accountRegistered.getAccountId());
		account.setType(this.accountRegistered.getType());
		account.setAccountShortCode(this.accountRegistered.getShortCode());
		account.setHasCustomLogo(false);
		account.setContactName(this.accountRegistered.getContactName());
		return account;
	}

}
