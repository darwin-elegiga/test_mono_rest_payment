package com.uhg.optum.financials.payments.payer.dto.subaccount;

import com.uhg.optum.financials.payments.organization.enums.MarketType;
import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.UUID;

@ExtendWith(MockitoExtension.class)
class SubAccountDtoTests {

	@InjectMocks
	private SubAccountDto subAccountDto;

	private SubAccountRegistered subAccountRegistered;

	@Test
	void testToEntity() {
		// Arrange
		this.subAccountRegistered = new SubAccountRegistered();
		this.subAccountRegistered.setMarketType(MarketType.MEDICAL);
		this.subAccountRegistered.setName("Name");
		this.subAccountRegistered.setFaxNumber("FaxNumber");
		this.subAccountRegistered.setPhoneNumber("PhoneNumber");
		this.subAccountRegistered.setContactEmail("ContactEmail");
		this.subAccountRegistered.setTin("Tin");
		this.subAccountRegistered.setHoldAndRelease(true);
		this.subAccountRegistered.setProgramId("ProgramId");
		this.subAccountRegistered.setPayer835Id("Payer835Id");
		this.subAccountRegistered.setClaimOfficePhoneNumber("ClaimOfficePhoneNumber");
		this.subAccountRegistered.setClaimOfficePhoneNumberExtension("ClaimOfficePhoneNumberExtension");
		this.subAccountRegistered.setClaimOfficeUrl("ClaimOfficeURL");
		this.subAccountRegistered.setTechnicalContactName("TechnicalContactName");
		this.subAccountRegistered.setTechnicalPhoneNumber("TechnicalPhoneNumber");
		this.subAccountRegistered.setTechnicalPhoneNumberExtension("TechnicalPhoneNumberExtension");
		this.subAccountRegistered.setTechnicalUrl("TechnicalURL");
		this.subAccountRegistered.setTechnicalFaxNumber("TechnicalFaxNumber");
		this.subAccountRegistered.setTechnicalEmailAddress("TechnicalEmailAddress");
		com.uhg.optum.financials.payments.organization.models.Address addressDto = new com.uhg.optum.financials.payments.organization.models.Address();
		addressDto.setPostalCode("12345");
		addressDto.setCity("City");
		addressDto.setAddressLine2("Line2");
		addressDto.setAddressLine1("Line1");

		this.subAccountRegistered.setAddress(addressDto);
		this.subAccountRegistered.setSubAccountId(UUID.randomUUID());
		this.subAccountRegistered.setType("Type");

		com.uhg.optum.financials.payments.organization.models.FundingAccounts fundingAccounts1 = new com.uhg.optum.financials.payments.organization.models.FundingAccounts();
		fundingAccounts1.setFundingLocalAccount(UUID.randomUUID());
		fundingAccounts1.setFundingForeignAccount(UUID.randomUUID());
		fundingAccounts1.setUnloadForeignAccount(UUID.randomUUID());
		fundingAccounts1.setUnloadLocalAccount(UUID.randomUUID());

		this.subAccountRegistered.setFundingAccounts(fundingAccounts1);
		this.subAccountDto.setSubAccountRegistered(this.subAccountRegistered);
		SubAccount subAccount = new SubAccount();
	}

}
