package com.uhg.optum.financials.payments.payer.mapper;

import com.uhg.optum.financials.payments.organization.enums.MarketType;
import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import com.uhg.optum.financials.payments.organization.events.SubAccountUpdated;
import com.uhg.optum.financials.payments.organization.models.Address;
import com.uhg.optum.financials.payments.organization.models.FundingAccounts;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import org.springframework.util.ObjectUtils;

import java.util.Optional;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.DEFAULT_HAS_CUSTOM_LOGO;

public final class SubAccountMapper {

	private SubAccountMapper() {

	}

	public static SubAccount toEntity(SubAccountRegistered subAccountRegistered) {
		SubAccount entity = new SubAccount();
		entity.setMarketType(String.valueOf(subAccountRegistered.getMarketType()));
		entity.setName(subAccountRegistered.getName());
		entity.setAccountId(subAccountRegistered.getAccountId());
		entity.setFaxNumber(subAccountRegistered.getFaxNumber());
		entity.setPhoneNumber(subAccountRegistered.getPhoneNumber());
		entity.setContactEmail(subAccountRegistered.getContactEmail());
		entity.setTin(subAccountRegistered.getTin());
		entity.setHoldAndRelease(subAccountRegistered.isHoldAndRelease());
		entity.setProgramId(subAccountRegistered.getProgramId());
		entity.setPayer835Id(subAccountRegistered.getPayer835Id());
		entity.setClaimOfficePhoneNumber(subAccountRegistered.getClaimOfficePhoneNumber());
		entity.setClaimOfficePhoneNumberExtension(subAccountRegistered.getClaimOfficePhoneNumberExtension());
		entity.setClaimOfficeURL(subAccountRegistered.getClaimOfficeUrl());
		entity.setTechnicalContactName(subAccountRegistered.getTechnicalContactName());
		entity.setTechnicalPhoneNumber(subAccountRegistered.getTechnicalPhoneNumber());
		entity.setTechnicalPhoneNumberExtension(subAccountRegistered.getTechnicalPhoneNumberExtension());
		entity.setTechnicalURL(subAccountRegistered.getTechnicalUrl());
		entity.setTechnicalFaxNumber(subAccountRegistered.getTechnicalFaxNumber());
		entity.setTechnicalEmailAddress(subAccountRegistered.getTechnicalEmailAddress());
		entity.setAddress(mapEntityAddress(subAccountRegistered.getAddress()));
		entity.setSubAccountId(subAccountRegistered.getSubAccountId());
		entity.setType(subAccountRegistered.getType());
		entity.setAccountShortCode(subAccountRegistered.getAccountShortCode());
		entity.setSubAccountShortCode(subAccountRegistered.getShortCode());
		entity.setHasCustomLogo(DEFAULT_HAS_CUSTOM_LOGO);
		entity.setContactName(subAccountRegistered.getContactName());
		entity.setReturnMailingAddress(mapEntityAddress(subAccountRegistered.getReturnMailingAddress()));
		return entity;
	}

	public static SubAccount toEntity(SubAccountUpdated subAccountUpdated, SubAccount subAccount) {
		subAccount = Optional.ofNullable(subAccount).orElse(new SubAccount());
		subAccount.setMarketType(String.valueOf(subAccountUpdated.getMarketType()));
		subAccount.setName(subAccountUpdated.getName());
		subAccount.setFaxNumber(subAccountUpdated.getFaxNumber());
		subAccount.setPhoneNumber(subAccountUpdated.getPhoneNumber());
		subAccount.setContactEmail(subAccountUpdated.getContactEmail());
		subAccount.setTin(subAccountUpdated.getTin());
		subAccount.setHoldAndRelease(subAccountUpdated.isHoldAndRelease());
		subAccount.setProgramId(subAccountUpdated.getProgramId());
		subAccount.setPayer835Id(subAccountUpdated.getPayer835Id());
		subAccount.setClaimOfficePhoneNumber(subAccountUpdated.getClaimOfficePhoneNumber());
		subAccount.setClaimOfficePhoneNumberExtension(subAccountUpdated.getClaimOfficePhoneNumberExtension());
		subAccount.setClaimOfficeURL(subAccountUpdated.getClaimOfficeUrl());
		subAccount.setTechnicalContactName(subAccountUpdated.getTechnicalContactName());
		subAccount.setTechnicalPhoneNumber(subAccountUpdated.getTechnicalPhoneNumber());
		subAccount.setTechnicalPhoneNumberExtension(subAccountUpdated.getTechnicalPhoneNumberExtension());
		subAccount.setTechnicalURL(subAccountUpdated.getTechnicalUrl());
		subAccount.setTechnicalFaxNumber(subAccountUpdated.getTechnicalFaxNumber());
		subAccount.setTechnicalEmailAddress(subAccountUpdated.getTechnicalEmailAddress());
		subAccount.setAddress(subAccountUpdated.getAddress());
		subAccount.setType(subAccountUpdated.getType());
		subAccount.setFundingMethod(ObjectUtils.isEmpty(subAccountUpdated.getFundingMethod()) ? null
				: String.valueOf(subAccountUpdated.getFundingMethod()));
		subAccount.setContactName(subAccountUpdated.getContactName());
		subAccount.setReturnMailingAddress(subAccountUpdated.getReturnMailingAddress());

		return subAccount;
	}

	public static SubAccountDto toDto(SubAccount entity, SubAccountDto dto) {
		dto = Optional.ofNullable(dto).orElse(new SubAccountDto());

		SubAccountRegistered subAccountRegistered = new SubAccountRegistered();
		subAccountRegistered.setAccountId(entity.getAccountId());
		subAccountRegistered.setAccountShortCode(entity.getAccountShortCode());
		subAccountRegistered.setSubAccountId(entity.getSubAccountId());
		subAccountRegistered.setTin(entity.getTin());
		subAccountRegistered.setMarketType(MarketType.valueOf(entity.getMarketType()));
		subAccountRegistered.setName(entity.getName());
		subAccountRegistered.setFaxNumber(entity.getFaxNumber());
		subAccountRegistered.setPhoneNumber(entity.getPhoneNumber());
		subAccountRegistered.setContactEmail(entity.getContactEmail());
		subAccountRegistered.setHoldAndRelease(entity.getHoldAndRelease());
		subAccountRegistered.setProgramId(entity.getProgramId());
		subAccountRegistered.setPayer835Id(entity.getPayer835Id());
		subAccountRegistered.setClaimOfficePhoneNumber(entity.getClaimOfficePhoneNumber());
		subAccountRegistered.setClaimOfficePhoneNumberExtension(entity.getClaimOfficePhoneNumberExtension());
		subAccountRegistered.setClaimOfficeUrl(entity.getClaimOfficeURL());
		subAccountRegistered.setTechnicalContactName(entity.getTechnicalContactName());
		subAccountRegistered.setTechnicalPhoneNumber(entity.getTechnicalPhoneNumber());
		subAccountRegistered.setTechnicalPhoneNumberExtension(entity.getTechnicalPhoneNumberExtension());
		subAccountRegistered.setTechnicalUrl(entity.getTechnicalURL());
		subAccountRegistered.setTechnicalFaxNumber(entity.getTechnicalFaxNumber());
		subAccountRegistered.setTechnicalEmailAddress(entity.getTechnicalEmailAddress());
		subAccountRegistered.setAddress(mapAddress(entity.getAddress()));
		subAccountRegistered.setType(entity.getType());
		subAccountRegistered.setShortCode(entity.getSubAccountShortCode());
		subAccountRegistered.setFundingAccounts(mapFundingAccounts(entity));
		subAccountRegistered.setContactName(entity.getContactName());
		subAccountRegistered.setCreatedBy(entity.getCreatedById());
		subAccountRegistered.setReturnMailingAddress(mapAddress(entity.getReturnMailingAddress()));
		dto.setSubAccountRegistered(subAccountRegistered);
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

	private static Address mapEntityAddress(Address payerAddress) {
		if (payerAddress == null) {
			return null;
		}
		Address organizationAddress = new Address();
		organizationAddress.setAddressLine1(payerAddress.getAddressLine1());
		organizationAddress.setAddressLine2(payerAddress.getAddressLine2());
		organizationAddress.setCity(payerAddress.getCity());
		organizationAddress.setState(payerAddress.getState());
		organizationAddress.setPostalCode(payerAddress.getPostalCode());
		return organizationAddress;
	}

	private static FundingAccounts mapFundingAccounts(SubAccount subAccount) {
		FundingAccounts fundingAccounts = new FundingAccounts();
		fundingAccounts.setFundingForeignAccount(subAccount.getFundingForeignAccount());
		fundingAccounts.setFundingLocalAccount(subAccount.getFundingLocalAccount());
		fundingAccounts.setUnloadForeignAccount(subAccount.getUnloadForeignAccount());
		fundingAccounts.setUnloadLocalAccount(subAccount.getUnloadLocalAccount());
		fundingAccounts.setPayFromBankAccount(subAccount.getPayFromBankAccount());
		return fundingAccounts;
	}

}
