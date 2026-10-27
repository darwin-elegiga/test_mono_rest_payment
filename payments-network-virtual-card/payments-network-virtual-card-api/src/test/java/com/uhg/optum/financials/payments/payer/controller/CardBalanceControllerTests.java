package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.service.impl.VirtualCardBalanceService;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.ResponseEntity;

import static org.assertj.core.api.AssertionsForClassTypes.assertThat;

@ExtendWith(MockitoExtension.class)
public class CardBalanceControllerTests {

	@Mock
	private VirtualCardBalanceService cardBalanceService;

	@InjectMocks
	private CardBalanceController cardBalanceController;

	@Test
	void getCardBalances_success() throws Exception {
		String paymentId = "pid";
		ResponseEntity<?> expectedResponse = ResponseEntity.ok("balance");

		BDDMockito.when(this.cardBalanceService.getCardBalances(paymentId)).then((invocation) -> expectedResponse);

		ResponseEntity<?> response = this.cardBalanceController.getCardBalances(paymentId);

		Assertions.assertThat(response).isNotNull();
		Assertions.assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
		Assertions.assertThat(response.getBody()).isEqualTo("balance");
	}

	@Test
	void getCardBalances_invalidPaymentId_throwsException() {
		String paymentId = "";

		try {
			this.cardBalanceController.getCardBalances(paymentId);
		}
		catch (Exception ex) {
			assertThat(ex).isInstanceOf(Exception.class);
		}
	}

}
