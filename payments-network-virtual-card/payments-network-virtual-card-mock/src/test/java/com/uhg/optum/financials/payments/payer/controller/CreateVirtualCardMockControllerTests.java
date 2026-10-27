package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.CancelCardRequest;
import com.uhg.optum.financials.payments.payer.dto.CreateCardRequest;
import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import org.slf4j.Logger;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import static org.assertj.core.api.BDDAssertions.then;

class CreateVirtualCardMockControllerTests {

	@Mock
	private Logger log;

	@InjectMocks
	private CreateVirtualCardMockController createVirtualCardMockController;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	void shouldCreateCardsSuccessfully() {
		CreateCardRequest request = org.mockito.Mockito.mock(CreateCardRequest.class);
		ResponseEntity<String> response = this.createVirtualCardMockController.createCards(request);
		then(response.getStatusCode()).isEqualTo(HttpStatus.ACCEPTED);
		then(response.getBody()).isEqualTo("Virtual card created successfully");
	}

	@Test
	void shouldCancelCardSuccessfully() {
		CancelCardRequest request = org.mockito.Mockito.mock(CancelCardRequest.class);
		ResponseEntity<String> response = this.createVirtualCardMockController.cancelCard(request);
		then(response.getStatusCode()).isEqualTo(HttpStatus.ACCEPTED);
		then(response.getBody()).isEqualTo("Virtual card cancelled successfully");
	}

	@Test
	void shouldResetCardSuccessfully() {
		ResetCardRequest request = org.mockito.Mockito.mock(ResetCardRequest.class);
		ResponseEntity<String> response = this.createVirtualCardMockController.resetCard(request);
		then(response.getStatusCode()).isEqualTo(HttpStatus.ACCEPTED);
		then(response.getBody()).isEqualTo("Virtual card reset successfully");
	}

}
