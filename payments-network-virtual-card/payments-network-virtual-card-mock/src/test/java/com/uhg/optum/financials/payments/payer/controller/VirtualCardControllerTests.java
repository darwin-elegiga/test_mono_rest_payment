package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
public class VirtualCardControllerTests {

	@InjectMocks
	VirtualCardController virtualCardController;

	@Mock
	CardCreationRepository cardCreationRepository;

	@Test
	public void testVirtualCardController() {
		CardCreation cardCreation = new CardCreation();
		BDDMockito.when(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(any()))
			.thenReturn(Optional.of(cardCreation));
		ResponseEntity<?> response = this.virtualCardController.getCardCreationDetails("123654");
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
	}

	@Test
	public void testException() {
		BDDMockito.when(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(any()))
			.thenReturn(Optional.empty());
		assertThatThrownBy(() -> this.virtualCardController.getCardCreationDetails("123654"))
			.isInstanceOf(com.uhg.optum.financials.payments.payer.exception.NotFoundException.class)
			.hasMessage("No Record found for card creation details");
	}

}
