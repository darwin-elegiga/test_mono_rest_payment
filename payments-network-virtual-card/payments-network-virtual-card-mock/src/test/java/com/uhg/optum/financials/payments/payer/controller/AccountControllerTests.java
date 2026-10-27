package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.AccountRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.AccountUpdatedDto;
import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.model.Account;
import com.uhg.optum.financials.payments.payer.service.AccountPublisherService;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import static org.assertj.core.api.BDDAssertions.then;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.willThrow;
import static org.mockito.Mockito.verify;

@ExtendWith(MockitoExtension.class)
class AccountControllerTests {

	@Mock
	private AccountPublisherService accountPublisherService;

	@InjectMocks
	private AccountController accountController;

	@Test
	void shouldProduceAccountRegistered() {
		AccountRegisteredEvent event = new AccountRegisteredEvent();
		String expectedResponse = "registered";
		given(this.accountPublisherService.publishAccountRegisteredEvent(event)).willReturn(expectedResponse);

		ResponseEntity<?> response = this.accountController.produceAccountRegistered(event);

		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).isEqualTo(expectedResponse);
		verify(this.accountPublisherService).publishAccountRegisteredEvent(event);
	}

	@Test
	void shouldProduceAccountUpdated() {
		AccountUpdatedDto event = new AccountUpdatedDto();
		String expectedResponse = "updated";
		given(this.accountPublisherService.publishAccountUpdatedEvent(event)).willReturn(expectedResponse);

		ResponseEntity<?> response = this.accountController.produceAccountUpdated(event);

		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).isEqualTo(expectedResponse);
		verify(this.accountPublisherService).publishAccountUpdatedEvent(event);
	}

	@Test
	void shouldGetAccountSuccessfully() {
		String accountId = "acc123";
		Account account = new Account();
		given(this.accountPublisherService.getAccountRegDetails(accountId)).willReturn(account);

		ResponseEntity<?> response = this.accountController.getAccount(accountId);

		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).isEqualTo(account);
		verify(this.accountPublisherService).getAccountRegDetails(accountId);
	}

	@Test
	void shouldGetAccount_whenAccountIdContainsControlChars() {
		String maliciousId = "acc123\r\ninjected-log-line";
		Account account = new Account();
		given(this.accountPublisherService.getAccountRegDetails(maliciousId)).willReturn(account);

		ResponseEntity<?> response = this.accountController.getAccount(maliciousId);

		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).isEqualTo(account);
		verify(this.accountPublisherService).getAccountRegDetails(maliciousId);
	}

	@Test
	void shouldThrowInternalServerExceptionWhenServiceFails() {
		String accountId = "notfound";
		willThrow(new InternalServerException("Database error occurred while fetching account details"))
			.given(this.accountPublisherService)
			.getAccountRegDetails(accountId);

		assertThatThrownBy(() -> this.accountController.getAccount(accountId))
			.isInstanceOf(InternalServerException.class)
			.hasMessageContaining("Database error occurred while fetching account details");
		verify(this.accountPublisherService).getAccountRegDetails(accountId);
	}

}
