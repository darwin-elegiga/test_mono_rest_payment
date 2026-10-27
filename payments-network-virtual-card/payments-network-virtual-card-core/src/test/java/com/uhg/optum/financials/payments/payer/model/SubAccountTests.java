package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.payments.organization.enums.FundingMethod;
import com.uhg.optum.financials.payments.organization.enums.MarketType;
import com.uhg.optum.financials.payments.organization.models.FundingAccounts;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.UUID;

import static org.assertj.core.api.AssertionsForClassTypes.assertThat;

@ExtendWith(MockitoExtension.class)
class SubAccountTests {

	@InjectMocks
	SubAccount subAccount;

	@BeforeEach
	void setUp() {
		this.subAccount = new SubAccount();
		this.subAccount.setAccountShortCode("testCode");
		this.subAccount.setSubAccountId(UUID.randomUUID());
		this.subAccount.setTin("Tin");
		this.subAccount.setMarketType("MarketType");
		this.subAccount.setName("Name");
		this.subAccount.setFaxNumber("FaxNumber");
		this.subAccount.setPhoneNumber("PhoneNumber");
		this.subAccount.setContactEmail("ContactEmail");
		this.subAccount.setHoldAndRelease(true);
		this.subAccount.setFundingMethod("FundingMethod");
		this.subAccount.setProgramId("ProgramId");
		this.subAccount.setPayer835Id("Payer835Id");
		this.subAccount.setClaimOfficePhoneNumber("ClaimOfficePhoneNumber");
		this.subAccount.setClaimOfficePhoneNumberExtension("ClaimOfficePhoneNumberExtension");
		this.subAccount.setClaimOfficeURL("ClaimOfficeURL");
		this.subAccount.setOrgShortCode("test");
		this.subAccount.setType("Type");
	}

	@Test
	void testToDto() {
		UUID subPayerId = UUID.randomUUID();
		String name = "Test SubPayer";
		String contactEmail = "testsubpayer@example.com";
		String phoneNumber = "1234567890";
		String faxNumber = "0987654321";
		String technicalContactName = "Tech Contact";
		String technicalPhoneNumber = "4445556666";
		String technicalPhoneNumberExtension = "456";
		String technicalURL = "http://technical.com";
		String technicalFaxNumber = "7778889999";
		String technicalEmailAddress = "tech@example.com";

		Address address = new Address();
		address.setLine1("AddressLine1");
		address.setLine2("AddressLine2");
		address.setCity("City");
		address.setState("State");
		address.setPostalCode("12345");
		this.subAccount.setFundingLocalAccount(UUID.randomUUID());
		this.subAccount.setFundingForeignAccount(UUID.randomUUID());
		this.subAccount.setUnloadForeignAccount(UUID.randomUUID());
		this.subAccount.setUnloadLocalAccount(UUID.randomUUID());
		this.subAccount.setAddress(address);
		this.subAccount.setSubAccountId(subPayerId);
		this.subAccount.setName(name);
		this.subAccount.setContactEmail(contactEmail);
		this.subAccount.setPhoneNumber(phoneNumber);
		this.subAccount.setFaxNumber(faxNumber);
		this.subAccount.setTechnicalContactName(technicalContactName);
		this.subAccount.setTechnicalPhoneNumber(technicalPhoneNumber);
		this.subAccount.setTechnicalPhoneNumberExtension(technicalPhoneNumberExtension);
		this.subAccount.setTechnicalURL(technicalURL);
		this.subAccount.setTechnicalFaxNumber(technicalFaxNumber);
		this.subAccount.setTechnicalEmailAddress(technicalEmailAddress);
		this.subAccount.setMarketType(MarketType.MEDICAL.toString());
		this.subAccount.setFundingMethod(FundingMethod.NON_FUNDED.toString());
		SubAccountDto dtoNew = this.subAccount.toDto(new SubAccountDto());
		Assertions.assertThat(dtoNew).isNotNull();
		Assertions.assertThat(dtoNew.getSubAccountRegistered()).isNotNull();

	}

	@Test
	void testToFundingDto() {

		FundingAccounts fundingAccountsDto = new FundingAccounts();
		FundingAccounts result = this.subAccount.toDto(fundingAccountsDto);
		assertThat(result).isNotNull();
		assertThat(result.getFundingForeignAccount()).isEqualTo(this.subAccount.getFundingForeignAccount());
		assertThat(result.getFundingLocalAccount()).isEqualTo(this.subAccount.getFundingLocalAccount());
		assertThat(result.getUnloadForeignAccount()).isEqualTo(this.subAccount.getUnloadForeignAccount());
		assertThat(result.getUnloadLocalAccount()).isEqualTo(this.subAccount.getUnloadLocalAccount());
	}

}
