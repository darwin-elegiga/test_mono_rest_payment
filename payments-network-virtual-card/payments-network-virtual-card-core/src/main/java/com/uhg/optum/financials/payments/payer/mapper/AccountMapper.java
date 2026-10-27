package com.uhg.optum.financials.payments.payer.mapper;

import com.uhg.optum.financials.payments.organization.enums.MarketType;
import com.uhg.optum.financials.payments.organization.events.AccountRegistered;
import com.uhg.optum.financials.payments.organization.events.AccountUpdated;
import com.uhg.optum.financials.payments.organization.models.Address;
import com.uhg.optum.financials.payments.payer.dto.account.AccountDto;
import com.uhg.optum.financials.payments.payer.model.Account;

import java.util.Optional;

public final class AccountMapper {

	private AccountMapper() {

	}

	public static Account toEntity(AccountRegistered accountRegistered) {
		Account account = new Account();

		account.setAccountId(accountRegistered.getAccountId());
		account.setMarketType(String.valueOf(accountRegistered.getMarketType()));
		account.setName(accountRegistered.getName());
		account.setFaxNumber(accountRegistered.getFaxNumber());
		account.setPhoneNumber(accountRegistered.getPhoneNumber());
		account.setContactEmail(accountRegistered.getContactEmail());
		account.setTin(accountRegistered.getTin());
		account.setHoldAndRelease(accountRegistered.isHoldAndRelease());
		account.setProgramId(accountRegistered.getProgramId());
		account.setPayer835Id(accountRegistered.getPayer835Id());
		account.setClaimOfficePhoneNumber(accountRegistered.getClaimOfficePhoneNumber());
		account.setClaimOfficePhoneNumberExtension(accountRegistered.getClaimOfficePhoneNumberExtension());
		account.setClaimOfficeURL(accountRegistered.getClaimOfficeUrl());
		account.setTechnicalContactName(accountRegistered.getTechnicalContactName());
		account.setTechnicalPhoneNumber(accountRegistered.getTechnicalPhoneNumber());
		account.setTechnicalPhoneNumberExtension(accountRegistered.getTechnicalPhoneNumberExtension());
		account.setTechnicalURL(accountRegistered.getTechnicalUrl());
		account.setTechnicalFaxNumber(accountRegistered.getTechnicalFaxNumber());
		account.setTechnicalEmailAddress(accountRegistered.getTechnicalEmailAddress());
		account.setAddress(accountRegistered.getAddress());
		account.setType(accountRegistered.getType());
		account.setAccountShortCode(accountRegistered.getShortCode());
		account.setContactName(accountRegistered.getContactName());
		return account;
	}

	public static Account toEntity(AccountUpdated accountUpdated, Account account) {
		account = Optional.ofNullable(account).orElse(new Account());

		account.setAccountId(accountUpdated.getAccountId());
		account.setMarketType(String.valueOf(accountUpdated.getMarketType()));
		account.setName(accountUpdated.getName());
		account.setFaxNumber(accountUpdated.getFaxNumber());
		account.setPhoneNumber(accountUpdated.getPhoneNumber());
		account.setContactEmail(accountUpdated.getContactEmail());
		account.setTin(accountUpdated.getTin());
		account.setHoldAndRelease(accountUpdated.isHoldAndRelease());
		account.setProgramId(accountUpdated.getProgramId());
		account.setPayer835Id(accountUpdated.getPayer835Id());
		account.setClaimOfficePhoneNumber(accountUpdated.getClaimOfficePhoneNumber());
		account.setClaimOfficePhoneNumberExtension(accountUpdated.getClaimOfficePhoneNumberExtension());
		account.setClaimOfficeURL(accountUpdated.getClaimOfficeUrl());
		account.setTechnicalContactName(accountUpdated.getTechnicalContactName());
		account.setTechnicalPhoneNumber(accountUpdated.getTechnicalPhoneNumber());
		account.setTechnicalPhoneNumberExtension(accountUpdated.getTechnicalPhoneNumberExtension());
		account.setTechnicalURL(accountUpdated.getTechnicalUrl());
		account.setTechnicalFaxNumber(accountUpdated.getTechnicalFaxNumber());
		account.setTechnicalEmailAddress(accountUpdated.getTechnicalEmailAddress());
		account.setAddress(accountUpdated.getAddress());
		account.setType(accountUpdated.getType());
		account.setAccountShortCode(accountUpdated.getShortCode());
		account.setContactName(accountUpdated.getContactName());
		account.setFundingMethod(String.valueOf(accountUpdated.getFundingMethod()));
		return account;
	}

	public static AccountDto toDto(Account entity, AccountDto dto) {
		dto = Optional.ofNullable(dto).orElse(new AccountDto());
		AccountRegistered accountRegistered = new AccountRegistered();

		accountRegistered.setAccountId(entity.getAccountId());
		accountRegistered.setTin(entity.getTin());
		accountRegistered.setMarketType(MarketType.valueOf(entity.getMarketType()));
		accountRegistered.setName(entity.getName());
		accountRegistered.setFaxNumber(entity.getFaxNumber());
		accountRegistered.setPhoneNumber(entity.getPhoneNumber());
		accountRegistered.setContactEmail(entity.getContactEmail());
		accountRegistered.setHoldAndRelease(entity.getHoldAndRelease());
		accountRegistered.setProgramId(entity.getProgramId());
		accountRegistered.setPayer835Id(entity.getPayer835Id());
		accountRegistered.setClaimOfficePhoneNumber(entity.getClaimOfficePhoneNumber());
		accountRegistered.setClaimOfficePhoneNumberExtension(entity.getClaimOfficePhoneNumberExtension());
		accountRegistered.setClaimOfficeUrl(entity.getClaimOfficeURL());
		accountRegistered.setTechnicalContactName(entity.getTechnicalContactName());
		accountRegistered.setTechnicalPhoneNumber(entity.getTechnicalPhoneNumber());
		accountRegistered.setTechnicalPhoneNumberExtension(entity.getTechnicalPhoneNumberExtension());
		accountRegistered.setTechnicalUrl(entity.getTechnicalURL());
		accountRegistered.setTechnicalFaxNumber(entity.getTechnicalFaxNumber());
		accountRegistered.setTechnicalEmailAddress(entity.getTechnicalEmailAddress());
		accountRegistered.setType(entity.getType());
		accountRegistered.setShortCode(entity.getAccountShortCode());
		accountRegistered.setContactName(entity.getContactName());
		accountRegistered.setAddress(mapAddress(entity.getAddress()));
		accountRegistered.setCreatedBy(entity.getCreatedById());
		dto.setAccountRegistered(accountRegistered);
		return dto;
	}

	private static Address mapAddress(com.uhg.optum.financials.payments.payer.model.Address payerAddress) {
		if (payerAddress == null) {
			return null;
		}
		Address organizationAddress = new Address();
		organizationAddress.setAddressLine1(payerAddress.getLine1());
		organizationAddress.setAddressLine2(payerAddress.getLine2());
		organizationAddress.setCity(payerAddress.getCity());
		organizationAddress.setState(payerAddress.getState());
		organizationAddress.setPostalCode(payerAddress.getPostalCode());
		return organizationAddress;
	}

}
