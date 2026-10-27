package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.payments.organization.enums.MarketType;
import com.uhg.optum.financials.payments.payer.dto.account.AccountDto;
import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

import java.util.UUID;

class AccountTests {

	@Test
	void testToDto_withNullInput() {
		Account thisAccount = new Account();
		thisAccount.setAccountId(UUID.randomUUID());
		thisAccount.setAccountShortCode("SHORTCODE");
		thisAccount.setOnboardingStatus("ONBOARDED");
		thisAccount.setMarketType(MarketType.MEDICAL.name());
		thisAccount.setName("Test Name");
		thisAccount.setContactEmail("test@email.com");
		thisAccount.setType("TYPE");
		thisAccount.setHoldAndRelease(Boolean.TRUE);
		Address address = new Address();
		address.setLine1("123 Main St");
		thisAccount.setAddress(address);
		AccountDto result = thisAccount.toDto((AccountDto) null);
		assertThat(result).isNotNull();
		assertThat(result.getAccountRegistered()).isNotNull();
		assertThat(result.getAccountRegistered().getAccountId()).isEqualTo(thisAccount.getAccountId());
		assertThat(result.getAccountRegistered().getShortCode()).isEqualTo(thisAccount.getAccountShortCode());
		assertThat(result.getAccountRegistered().getName()).isEqualTo(thisAccount.getName());
		assertThat(result.getAccountRegistered().getContactEmail()).isEqualTo(thisAccount.getContactEmail());
		assertThat(result.getAccountRegistered().getType()).isEqualTo(thisAccount.getType());
		assertThat(result.getAccountRegistered().getOnboardingStatus()).isNull();
		assertThat(result.getAccountRegistered().getMarketType()).isEqualTo(MarketType.MEDICAL);
	}

	@Test
	void testToDto_withExistingAccountDto() {
		Account thisAccount = new Account();
		thisAccount.setAccountId(UUID.randomUUID());
		thisAccount.setAccountShortCode("SHORTCODE");
		thisAccount.setMarketType(MarketType.MEDICAL.name());
		thisAccount.setHoldAndRelease(Boolean.TRUE);
		Address address = new Address();
		address.setLine1("123 Main St");
		thisAccount.setAddress(address);
		AccountDto inputDto = new AccountDto();
		AccountDto result = thisAccount.toDto(inputDto);
		assertThat(result).isSameAs(inputDto);
		assertThat(result.getAccountRegistered()).isNotNull();
		assertThat(result.getAccountRegistered().getShortCode()).isEqualTo(thisAccount.getAccountShortCode());
		assertThat(result.getAccountRegistered().getMarketType()).isEqualTo(MarketType.MEDICAL);
	}

	@Test
	void testToDto_withNullAccountShortCode() {
		Account thisAccount = new Account();
		thisAccount.setAccountId(UUID.randomUUID());
		thisAccount.setAccountShortCode(null);
		thisAccount.setMarketType(MarketType.MEDICAL.name());
		thisAccount.setHoldAndRelease(Boolean.TRUE);
		Address address = new Address();
		address.setLine1("123 Main St");
		thisAccount.setAddress(address);
		AccountDto result = thisAccount.toDto((AccountDto) null);
		assertThat(result.getAccountRegistered().getShortCode()).isNull();
	}

	@Test
	void testToDto_withValidMarketType() {
		Account thisAccount = new Account();
		thisAccount.setAccountId(UUID.randomUUID());
		thisAccount.setAccountShortCode("SHORTCODE");
		thisAccount.setMarketType("MEDICAL");
		thisAccount.setHoldAndRelease(Boolean.TRUE);
		Address address = new Address();
		address.setLine1("123 Main St");
		thisAccount.setAddress(address);
		AccountDto result = thisAccount.toDto((AccountDto) null);
		assertThat(result.getAccountRegistered().getMarketType())
			.isEqualTo(com.uhg.optum.financials.payments.organization.enums.MarketType.MEDICAL);
	}

	@Test
	void testToDto_withInvalidMarketType_shouldThrowException() {
		Account thisAccount = new Account();
		thisAccount.setAccountId(UUID.randomUUID());
		thisAccount.setAccountShortCode("SHORTCODE");
		thisAccount.setMarketType("INVALID_TYPE");
		thisAccount.setHoldAndRelease(Boolean.TRUE);
		Address address = new Address();
		address.setLine1("123 Main St");
		thisAccount.setAddress(address);
		assertThatThrownBy(() -> thisAccount.toDto((AccountDto) null)).isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("No enum constant");
	}

}
