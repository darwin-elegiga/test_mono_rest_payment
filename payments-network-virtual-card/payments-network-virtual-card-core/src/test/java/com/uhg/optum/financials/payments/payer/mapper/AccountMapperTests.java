package com.uhg.optum.financials.payments.payer.mapper;

import com.uhg.optum.financials.payments.organization.enums.MarketType;
import com.uhg.optum.financials.payments.organization.events.AccountRegistered;
import com.uhg.optum.financials.payments.organization.models.Address;
import com.uhg.optum.financials.payments.payer.dto.account.AccountDto;
import com.uhg.optum.financials.payments.payer.model.Account;
import org.junit.jupiter.api.Test;

import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;

class AccountMapperTests {

	@Test
	void testToEntity() {
		// Arrange
		UUID uuid = UUID.randomUUID();

		AccountRegistered accountRegistered = new AccountRegistered();
		accountRegistered.setAccountId(uuid);
		accountRegistered.setMarketType(MarketType.MEDICAL);
		accountRegistered.setName("Test Account");
		accountRegistered.setFaxNumber("123456789");
		accountRegistered.setPhoneNumber("987654321");
		accountRegistered.setContactEmail("test@example.com");
		accountRegistered.setAddress(new Address());
		accountRegistered.getAddress().setAddressLine1("123 Main St");
		accountRegistered.getAddress().setAddressLine2("Suite 100");
		accountRegistered.getAddress().setCity("Atlanta");
		accountRegistered.getAddress().setState("GA");
		accountRegistered.getAddress().setPostalCode("30303");

		// Act
		Account account = AccountMapper.toEntity(accountRegistered);

		// Assert
		assertThat(account).isNotNull();
		assertThat(account.getAccountId()).isEqualTo(uuid);
		assertThat(account.getMarketType()).isEqualTo(MarketType.MEDICAL.toString());
		assertThat(account.getName()).isEqualTo("Test Account");
		assertThat(account.getFaxNumber()).isEqualTo("123456789");
		assertThat(account.getPhoneNumber()).isEqualTo("987654321");
		assertThat(account.getContactEmail()).isEqualTo("test@example.com");

		assertThat(account.getAddress().getLine1()).isEqualTo("123 Main St");
		assertThat(account.getAddress().getLine2()).isEqualTo("Suite 100");
		assertThat(account.getAddress().getCity()).isEqualTo("Atlanta");
		assertThat(account.getAddress().getState()).isEqualTo("GA");
		assertThat(account.getAddress().getPostalCode()).isEqualTo("30303");
		assertThat(account.getAddress().getCountry()).isEqualTo("us");
	}

	@Test
	void testToDto() {
		// Arrange
		UUID uuid = UUID.randomUUID();
		Account account = new Account();
		account.setAccountId(uuid);
		account.setMarketType(MarketType.MEDICAL.toString());
		account.setName("Test Account");
		account.setFaxNumber("123456789");
		account.setPhoneNumber("987654321");
		account.setContactEmail("test@example.com");
		account.setHoldAndRelease(true);

		// Act
		AccountDto accountDto = AccountMapper.toDto(account, null);

		// Assert
		assertThat(accountDto).isNotNull();
		assertThat(accountDto.getAccountRegistered()).isNotNull();
		assertThat(accountDto.getAccountRegistered().getAccountId()).isEqualTo(uuid);
		assertThat(accountDto.getAccountRegistered().getMarketType()).isEqualTo(MarketType.MEDICAL);
		assertThat(accountDto.getAccountRegistered().getName()).isEqualTo("Test Account");
		assertThat(accountDto.getAccountRegistered().getFaxNumber()).isEqualTo("123456789");
		assertThat(accountDto.getAccountRegistered().getPhoneNumber()).isEqualTo("987654321");
		assertThat(accountDto.getAccountRegistered().getContactEmail()).isEqualTo("test@example.com");
	}

}
