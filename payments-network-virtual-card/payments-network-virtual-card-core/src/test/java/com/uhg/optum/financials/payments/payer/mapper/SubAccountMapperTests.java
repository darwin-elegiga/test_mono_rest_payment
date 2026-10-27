package com.uhg.optum.financials.payments.payer.mapper;

import com.uhg.optum.financials.payments.organization.enums.MarketType;
import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto;
import com.uhg.optum.financials.payments.payer.model.Address;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import org.junit.jupiter.api.Test;

import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;

class SubAccountMapperTests {

	@Test
	void testToEntity() {
		// Arrange
		UUID uuid = UUID.randomUUID();

		SubAccountRegistered subAccountRegistered = new SubAccountRegistered();
		subAccountRegistered.setMarketType(MarketType.MEDICAL);
		subAccountRegistered.setName("Test SubAccount");
		subAccountRegistered.setAccountId(uuid);
		subAccountRegistered.setFaxNumber("123456789");
		subAccountRegistered.setPhoneNumber("987654321");
		subAccountRegistered.setContactEmail("test@example.com");
		subAccountRegistered.setTin("123456789");
		subAccountRegistered.setHoldAndRelease(true);
		subAccountRegistered.setProgramId("PROG123");
		subAccountRegistered.setPayer835Id("PAYER123");
		subAccountRegistered.setClaimOfficePhoneNumber("9876543210");
		subAccountRegistered.setClaimOfficePhoneNumberExtension("123");
		subAccountRegistered.setClaimOfficeUrl("http://example.com");
		subAccountRegistered.setTechnicalContactName("Tech Contact");
		subAccountRegistered.setTechnicalPhoneNumber("1122334455");
		subAccountRegistered.setTechnicalPhoneNumberExtension("456");
		subAccountRegistered.setTechnicalUrl("http://tech.example.com");
		subAccountRegistered.setTechnicalFaxNumber("123456789");
		subAccountRegistered.setTechnicalEmailAddress("tech@example.com");
		subAccountRegistered.setType("Type1");
		subAccountRegistered.setShortCode("SUB123");

		// Initialize Address
		com.uhg.optum.financials.payments.organization.models.Address address = new com.uhg.optum.financials.payments.organization.models.Address();
		address.setAddressLine1("123 Main St");
		address.setAddressLine2("Suite 100");
		address.setCity("Test City");
		address.setState("TS");
		address.setPostalCode("12345");
		subAccountRegistered.setAddress(address);
		subAccountRegistered.setReturnMailingAddress(address);

		// Act
		SubAccount entity = SubAccountMapper.toEntity(subAccountRegistered);

		// Assert
		assertThat(entity).isNotNull();
		assertThat(entity.getAddress()).isNotNull();
		assertThat(entity.getAddress().getLine1()).isEqualTo("123 Main St");
		assertThat(entity.getAddress().getCity()).isEqualTo("Test City");
	}

	@Test
	void testToDto() {
		// Arrange
		UUID subAccountId = UUID.randomUUID();
		SubAccount entity = new SubAccount();
		entity.setMarketType(MarketType.MEDICAL.toString());
		entity.setName("Test SubAccount");
		entity.setAccountId(subAccountId);
		entity.setFaxNumber("123456789");
		entity.setPhoneNumber("987654321");
		entity.setContactEmail("test@example.com");
		entity.setTin("123456789");
		entity.setHoldAndRelease(true);
		entity.setProgramId("PROG123");
		entity.setPayer835Id("PAYER123");
		entity.setClaimOfficePhoneNumber("9876543210");
		entity.setClaimOfficePhoneNumberExtension("123");
		entity.setClaimOfficeURL("http://example.com");
		entity.setTechnicalContactName("Tech Contact");
		entity.setTechnicalPhoneNumber("1122334455");
		entity.setTechnicalPhoneNumberExtension("456");
		entity.setTechnicalURL("http://tech.example.com");
		entity.setTechnicalFaxNumber("123456789");
		entity.setTechnicalEmailAddress("tech@example.com");
		entity.setType("Type1");
		entity.setSubAccountShortCode("SUB123");

		// Initialize Address
		Address address = new Address();
		address.setLine1("123 Main St");
		address.setLine2("Suite 100");
		address.setCity("Test City");
		address.setState("TS");
		address.setPostalCode("12345");
		entity.setAddress(address);

		// Act
		SubAccountDto dto = new SubAccountDto();
		SubAccountMapper.toDto(entity, dto);

		// Assert
		assertThat(dto).isNotNull();
		assertThat(dto.getSubAccountRegistered()).isNotNull();
		assertThat(dto.getSubAccountRegistered().getAddress()).isNotNull();
		assertThat(dto.getSubAccountRegistered().getAddress().getAddressLine1()).isEqualTo("123 Main St");
		assertThat(dto.getSubAccountRegistered().getAddress().getCity()).isEqualTo("Test City");
	}

}
