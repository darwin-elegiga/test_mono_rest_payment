package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.dto.CardBalanceResponseDto;
import com.uhg.optum.financials.payments.payer.dto.CardBalanceSummaryDto;
import com.uhg.optum.financials.payments.payer.dto.ErrorResponseDto;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import java.math.BigDecimal;
import java.util.UUID;

import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.times;

class VirtualCardBalanceServiceTests {

	@Mock
	private VirtualCardsRepository virtualCardsRepository;

	@Mock
	private VirtualCardUtil virtualCardUtil;

	@Mock
	private VirtualCard virtualCard;

	@InjectMocks
	private VirtualCardBalanceService virtualCardBalanceService;

	@Mock
	private ZetaApiService zetaApiService;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	void updateBalanceForAuthorization_nullCardOrAmount_doesNothing() {
		this.virtualCardBalanceService.updateBalanceForAuthorization(null, BigDecimal.TEN, AppConstants.DEBIT, "12345");
		this.virtualCardBalanceService.updateBalanceForAuthorization(this.virtualCard, null, AppConstants.DEBIT,
				"12345");
		then(this.virtualCardsRepository).should(never()).save(this.virtualCard);
		then(this.virtualCardUtil).should(never()).saveVirtualCardHistory(this.virtualCard, "AUTH", "12345");
	}

	@Test
	void updateBalanceForAuthorization_validUpdate_balanceDecreases() {
		given(this.virtualCard.getAvailableBalance()).willReturn(BigDecimal.valueOf(100));
		BigDecimal authAmount = BigDecimal.valueOf(30);
		this.virtualCardBalanceService.updateBalanceForAuthorization(this.virtualCard, authAmount, AppConstants.DEBIT,
				"12345");
		then(this.virtualCard).should(times(1)).setAvailableBalance(BigDecimal.valueOf(70));
		then(this.virtualCardsRepository).should(times(1)).save(this.virtualCard);
		then(this.virtualCardUtil).should(times(1)).saveVirtualCardHistory(this.virtualCard, "AUTH", "12345");
	}

	@Test
	void updateBalanceForAuthorization_newBalanceNotLessThanCurrent_noUpdate() {
		given(this.virtualCard.getAvailableBalance()).willReturn(BigDecimal.valueOf(100));
		BigDecimal authAmount = BigDecimal.valueOf(-10); // increases balance
		this.virtualCardBalanceService.updateBalanceForAuthorization(this.virtualCard, authAmount, AppConstants.DEBIT,
				"12345");
		then(this.virtualCard).should(never()).setAvailableBalance(BigDecimal.valueOf(110));
		then(this.virtualCardsRepository).should(times(1)).save(this.virtualCard);
		then(this.virtualCardUtil).should(times(1)).saveVirtualCardHistory(this.virtualCard, "AUTH", "12345");
	}

	@Test
	void updateBalanceForClearing_nullArguments_doesNothing() {
		this.virtualCardBalanceService.updateBalanceForClearing(null, BigDecimal.TEN, "DEBIT", false, "12345");
		this.virtualCardBalanceService.updateBalanceForClearing(this.virtualCard, null, "DEBIT", false, "12345");
		this.virtualCardBalanceService.updateBalanceForClearing(this.virtualCard, BigDecimal.TEN, null, false, "12345");
		then(this.virtualCardsRepository).should(never()).save(this.virtualCard);
		then(this.virtualCardUtil).should(never()).saveVirtualCardHistory(this.virtualCard, "CLEAR", "12345");
	}

	@Test
	void updateBalanceForClearing_debitIndicator_balanceDecreases() {
		given(this.virtualCard.getCurrentBalance()).willReturn(BigDecimal.valueOf(200));
		given(this.virtualCard.getCardGuid()).willReturn(UUID.fromString("3ef30e43-902c-45b0-9ecf-22c916144310"));
		BigDecimal clearingAmount = BigDecimal.valueOf(50);
		this.virtualCardBalanceService.updateBalanceForClearing(this.virtualCard, clearingAmount, "DEBIT", false,
				"12345");
		then(this.virtualCard).should(times(1)).setCurrentBalance(BigDecimal.valueOf(150));
		then(this.virtualCardsRepository).should(times(1)).save(this.virtualCard);
		then(this.virtualCardUtil).should(times(1)).saveVirtualCardHistory(this.virtualCard, "CLEAR", "12345");
	}

	@Test
	void updateBalanceForClearing_creditIndicator_balanceIncreases() {
		given(this.virtualCard.getCurrentBalance()).willReturn(BigDecimal.valueOf(200));
		given(this.virtualCard.getAvailableBalance()).willReturn(BigDecimal.valueOf(80));
		given(this.virtualCard.getCardGuid()).willReturn(UUID.fromString("3ef30e43-902c-45b0-9ecf-22c916144310"));
		BigDecimal clearingAmount = BigDecimal.valueOf(20);
		this.virtualCardBalanceService.updateBalanceForClearing(this.virtualCard, clearingAmount, "CREDIT", false,
				"12345");
		then(this.virtualCard).should(times(1)).setCurrentBalance(BigDecimal.valueOf(220));
		then(this.virtualCard).should(times(1)).setAvailableBalance(BigDecimal.valueOf(100));
		then(this.virtualCardsRepository).should(times(1)).save(this.virtualCard);
		then(this.virtualCardUtil).should(times(1)).saveVirtualCardHistory(this.virtualCard, "Credit", "12345");
	}

	@Test
	void updateBalanceForClearing_unknownIndicator_logsWarning() {
		BigDecimal clearingAmount = BigDecimal.valueOf(10);
		this.virtualCardBalanceService.updateBalanceForClearing(this.virtualCard, clearingAmount, "UNKNOWN", false,
				"12345");
		then(this.virtualCardsRepository).should(never()).save(this.virtualCard);
		then(this.virtualCardUtil).should(never()).saveVirtualCardHistory(this.virtualCard, "CLEAR", "12345");
	}

	@Test
	void getCardBalances_cardNotFound_throwsNotFoundException() {
		given(this.virtualCardsRepository.findByPaymentId("pid")).willReturn(null);
		Assertions.assertThatThrownBy(() -> this.virtualCardBalanceService.getCardBalances("pid"))
			.isInstanceOf(NotFoundException.class);
	}

	@Test
	void getCardBalances_emptyBody_throwsRuntimeException() throws Exception {
		VirtualCard card = new VirtualCard();
		card.setCardGuid(java.util.UUID.randomUUID());
		given(this.virtualCardsRepository.findByPaymentId("pid")).willReturn(card);
		given(this.zetaApiService.getCardBalance(card.getCardGuid()))
			.willReturn(new ResponseEntity<>("", HttpStatus.OK));
		Assertions.assertThatRuntimeException().isThrownBy(() -> this.virtualCardBalanceService.getCardBalances("pid"));
	}

	@Test
	void getCardBalances_non2xxStatus_returnsErrorResponseDto() throws Exception {
		VirtualCard card = new VirtualCard();
		card.setCardGuid(java.util.UUID.randomUUID());
		given(this.virtualCardsRepository.findByPaymentId("pid")).willReturn(card);
		ErrorResponseDto errorDto = new ErrorResponseDto();
		errorDto.setErrorMessage("Invalid request");
		String errorJson = new com.google.gson.Gson().toJson(errorDto);
		given(this.zetaApiService.getCardBalance(card.getCardGuid()))
			.willReturn(new ResponseEntity<>(errorJson, HttpStatus.BAD_REQUEST));
		ResponseEntity<?> response = this.virtualCardBalanceService.getCardBalances("pid");
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
		Assertions.assertThat(response.getBody()).isInstanceOf(ErrorResponseDto.class);
	}

	@Test
	void getCardBalances_success_returnsCardBalanceResponseDto() throws Exception {
		VirtualCard card = new VirtualCard();
		card.setCardGuid(java.util.UUID.randomUUID());
		given(this.virtualCardsRepository.findByPaymentId("pid")).willReturn(card);
		CardBalanceResponseDto dto = new CardBalanceResponseDto();
		CardBalanceResponseDto.Money balance = new CardBalanceResponseDto.Money();
		balance.setCurrency("USD");
		balance.setValue(10000L);
		dto.setAvailableBalance(balance);
		dto.setCurrentBalance(balance);
		String json = new com.google.gson.Gson().toJson(dto);
		given(this.zetaApiService.getCardBalance(card.getCardGuid()))
			.willReturn(new ResponseEntity<>(json, HttpStatus.OK));
		ResponseEntity<?> response = this.virtualCardBalanceService.getCardBalances("pid");
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isInstanceOf(CardBalanceSummaryDto.class);
		Assertions.assertThat(((CardBalanceSummaryDto) response.getBody()).getAvailableBalance())
			.isEqualByComparingTo(String.valueOf(100.00));
	}

}
