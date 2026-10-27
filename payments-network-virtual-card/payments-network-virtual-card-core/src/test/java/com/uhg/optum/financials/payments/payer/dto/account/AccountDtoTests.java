package com.uhg.optum.financials.payments.payer.dto.account;

import com.uhg.optum.financials.payments.organization.events.AccountRegistered;
import com.uhg.optum.financials.payments.organization.models.Address;
import com.uhg.optum.financials.payments.payer.model.Account;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import static com.uhg.optum.financials.payments.organization.enums.MarketType.MEDICAL;
import static org.assertj.core.api.BDDAssertions.then;

import java.util.UUID;

class AccountDtoTests {

	private final AccountRegistered accountRegistered = new AccountRegistered();

	@Test
	@DisplayName("should map AccountRegistered to Account entity correctly")
	void shouldMapAccountRegisteredToAccountEntity() {
		// Arrange
		this.accountRegistered.setMarketType(MEDICAL); // Set as needed for your enum
		this.accountRegistered.setName("Test Name");
		this.accountRegistered.setFaxNumber("1234567890");
		this.accountRegistered.setPhoneNumber("0987654321");
		this.accountRegistered.setContactEmail("test@example.com");
		this.accountRegistered.setTin("987654321");
		this.accountRegistered.setHoldAndRelease(true);
		this.accountRegistered.setProgramId("ProgramId");
		this.accountRegistered.setPayer835Id("Payer835Id");
		this.accountRegistered.setClaimOfficePhoneNumber("1112223333");
		this.accountRegistered.setClaimOfficePhoneNumberExtension("1234");
		this.accountRegistered.setClaimOfficeUrl("https://claim.office");
		this.accountRegistered.setTechnicalContactName("Tech Contact");
		this.accountRegistered.setTechnicalPhoneNumber("2223334444");
		this.accountRegistered.setTechnicalPhoneNumberExtension("5678");
		this.accountRegistered.setTechnicalUrl("https://tech.url");
		this.accountRegistered.setTechnicalFaxNumber("3334445555");
		this.accountRegistered.setTechnicalEmailAddress("tech@example.com");

		// Provide a valid Address to avoid NullPointerException
		Address address = new Address();
		address.setAddressLine1("123 Main St");
		address.setAddressLine2("Suite 100");
		address.setCity("Dallas");
		address.setState("TX");
		address.setPostalCode("75001");
		this.accountRegistered.setAddress(address);

		UUID uuid = UUID.randomUUID();
		this.accountRegistered.setAccountId(uuid);
		this.accountRegistered.setType("account_registered");
		this.accountRegistered.setShortCode("SHORTCODE");
		this.accountRegistered.setContactName("Contact Name");

		AccountDto accountDto = new AccountDto();
		accountDto.setAccountRegistered(this.accountRegistered);

		// Act
		Account account = accountDto.toEntity(null);

		// Assert
		then(account.getMarketType()).isEqualTo(String.valueOf(this.accountRegistered.getMarketType()));
		then(account.getName()).isEqualTo(this.accountRegistered.getName());
		then(account.getFaxNumber()).isEqualTo(this.accountRegistered.getFaxNumber());
		then(account.getPhoneNumber()).isEqualTo(this.accountRegistered.getPhoneNumber());
		then(account.getContactEmail()).isEqualTo(this.accountRegistered.getContactEmail());
		then(account.getTin()).isEqualTo(this.accountRegistered.getTin());
		then(account.getHoldAndRelease()).isEqualTo(this.accountRegistered.isHoldAndRelease());
		then(account.getProgramId()).isEqualTo(this.accountRegistered.getProgramId());
		then(account.getPayer835Id()).isEqualTo(this.accountRegistered.getPayer835Id());
		then(account.getClaimOfficePhoneNumber()).isEqualTo(this.accountRegistered.getClaimOfficePhoneNumber());
		then(account.getClaimOfficePhoneNumberExtension())
			.isEqualTo(this.accountRegistered.getClaimOfficePhoneNumberExtension());
		then(account.getClaimOfficeURL()).isEqualTo(this.accountRegistered.getClaimOfficeUrl());
		then(account.getTechnicalContactName()).isEqualTo(this.accountRegistered.getTechnicalContactName());
		then(account.getTechnicalPhoneNumber()).isEqualTo(this.accountRegistered.getTechnicalPhoneNumber());
		then(account.getTechnicalPhoneNumberExtension())
			.isEqualTo(this.accountRegistered.getTechnicalPhoneNumberExtension());
		then(account.getTechnicalURL()).isEqualTo(this.accountRegistered.getTechnicalUrl());
		then(account.getTechnicalFaxNumber()).isEqualTo(this.accountRegistered.getTechnicalFaxNumber());
		then(account.getTechnicalEmailAddress()).isEqualTo(this.accountRegistered.getTechnicalEmailAddress());
		then(account.getAddress()).isNotNull();
		then(account.getAddress().getCity()).isEqualTo(this.accountRegistered.getAddress().getCity());
		then(account.getAddress().getState()).isEqualTo(this.accountRegistered.getAddress().getState());
		then(account.getAddress().getPostalCode()).isEqualTo(this.accountRegistered.getAddress().getPostalCode());
		then(account.getAccountId()).isEqualTo(this.accountRegistered.getAccountId());
		then(account.getType()).isEqualTo(this.accountRegistered.getType());
		then(account.getAccountShortCode()).isEqualTo(this.accountRegistered.getShortCode());
		then(account.getHasCustomLogo()).isFalse();
		then(account.getContactName()).isEqualTo(this.accountRegistered.getContactName());
	}

}
