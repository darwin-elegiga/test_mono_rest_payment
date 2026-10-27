package com.uhg.optum.financials.payments.payer.controller;

import static org.assertj.core.api.BDDAssertions.then;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.willThrow;

import com.uhg.optum.financials.payments.payer.dto.SubAccountRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.SubAccountUpdatedDto;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.service.SubAccountPublisherService;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

@ExtendWith(MockitoExtension.class)
class SubAccountControllerTests {

	@Mock
	private SubAccountPublisherService subAccountPublisherService;

	@InjectMocks
	private SubAccountController subAccountController;

	@Test
	void testProduceSubAccountRegisteredReturnsOk() {
		SubAccountRegisteredEvent event = new SubAccountRegisteredEvent();
		given(this.subAccountPublisherService.publishSubAccountRegisteredEvent(event)).willReturn("registered");
		ResponseEntity<?> response = this.subAccountController.produceSubAccountRegistered(event);
		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).isEqualTo("registered");
	}

	@Test
	void testProduceSubAccountUpdatedReturnsOk() {
		SubAccountUpdatedDto dto = new SubAccountUpdatedDto();
		given(this.subAccountPublisherService.publishSubAccountUpdatedEvent(dto)).willReturn("updated");
		ResponseEntity<?> response = this.subAccountController.produceSubAccountUpdated(dto);
		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).isEqualTo("updated");
	}

	@Test
	void testGetSubAccountReturnsOk() {
		SubAccount subAccount = new SubAccount();
		String subAccountId = "SUB123";
		given(this.subAccountPublisherService.getSubAccountRegDetails(subAccountId)).willReturn(subAccount);
		ResponseEntity<?> response = this.subAccountController.getSubAccount(subAccountId);
		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).isSameAs(subAccount);
	}

	@Test
	void testGetSubAccount_whenSubAccountIdContainsControlChars() {
		String maliciousId = "SUB123\r\ninjected-log-line";
		SubAccount subAccount = new SubAccount();
		given(this.subAccountPublisherService.getSubAccountRegDetails(maliciousId)).willReturn(subAccount);
		ResponseEntity<?> response = this.subAccountController.getSubAccount(maliciousId);
		then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		then(response.getBody()).isSameAs(subAccount);
	}

	@Test
	void testGetSubAccountThrowsException() {
		String subAccountId = "SUB404";
		willThrow(new RuntimeException("Not found")).given(this.subAccountPublisherService)
			.getSubAccountRegDetails(subAccountId);
		assertThatThrownBy(() -> this.subAccountController.getSubAccount(subAccountId))
			.isInstanceOf(RuntimeException.class)
			.hasMessage("Not found");
	}

}
