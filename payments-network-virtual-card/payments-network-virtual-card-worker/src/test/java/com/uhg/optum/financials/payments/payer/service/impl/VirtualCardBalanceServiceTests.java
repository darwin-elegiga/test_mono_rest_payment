package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import java.math.BigDecimal;
import java.util.UUID;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.never;

@ExtendWith(MockitoExtension.class)
class VirtualCardBalanceServiceTests {

	private VirtualCardsRepository virtualCardsRepository;

	private VirtualCardUtil virtualCardUtil;

	private VirtualCardBalanceService service;

	private ZetaApiService zetaApiService;

	private RetryableApiService retryableApiService;

	@BeforeEach
	void setUp() {
		this.virtualCardsRepository = BDDMockito.mock(VirtualCardsRepository.class);
		this.virtualCardUtil = BDDMockito.mock(VirtualCardUtil.class);
		this.zetaApiService = BDDMockito.mock(ZetaApiService.class);
		this.retryableApiService = BDDMockito.mock(RetryableApiService.class);
		this.service = new VirtualCardBalanceService(this.virtualCardsRepository, this.virtualCardUtil,
				this.zetaApiService, this.retryableApiService);
	}

	@Test
	void updateBalanceForAuthorization_nullInputs_noUpdate() {
		this.service.updateBalanceForAuthorization(null, BigDecimal.TEN, AppConstants.DEBIT, "12345");
		this.service.updateBalanceForAuthorization(new VirtualCard(), null, AppConstants.CREDIT, "12345");
		BDDMockito.verify(this.virtualCardsRepository, never()).save(any());
		BDDMockito.verify(this.virtualCardUtil, never()).saveVirtualCardHistory(any(), any());
	}

	@Test
	void updateBalanceForAuthorization_validInput_balanceUpdated() {
		VirtualCard card = new VirtualCard();
		card.setAvailableBalance(BigDecimal.valueOf(100));
		card.setCurrentBalance(BigDecimal.valueOf(100));
		card.setCardGuid(UUID.randomUUID());
		card.setStatus("ACTIVE");
		card.setPaymentId("pid");
		card.setInitialBalance(BigDecimal.valueOf(100));
		card.setCurrency("USD");

		this.service.updateBalanceForAuthorization(card, BigDecimal.valueOf(30), AppConstants.DEBIT, "12345");

		Assertions.assertThat(card.getAvailableBalance()).isEqualByComparingTo(BigDecimal.valueOf(70));
		BDDMockito.verify(this.virtualCardsRepository).save(card);
		BDDMockito.verify(this.virtualCardUtil).saveVirtualCardHistory(any(), any(), any());
	}

	@Test
	void updateBalance_nullInputs_noUpdate() {
		this.service.updateBalanceForClearing(null, BigDecimal.TEN, AppConstants.DEBIT, false, "12345");
		this.service.updateBalanceForClearing(new VirtualCard(), null, AppConstants.CREDIT, false, "12345");
		this.service.updateBalanceForClearing(new VirtualCard(), BigDecimal.TEN, null, false, "12345");

		// Verify that repository save was never called
		BDDMockito.verify(this.virtualCardsRepository, never()).save(any());
		BDDMockito.verify(this.virtualCardUtil, never()).saveVirtualCardHistory(any(), any(), Mockito.isNull());
	}

	/*
	 * @Test void updateBalanceForClearing_nullInputs_noUpdate() {
	 * this.service.updateBalanceForClearing(null, new ClearExtract());
	 * this.service.updateBalanceForClearing(new VirtualCard(), null); ClearExtract ce =
	 * new ClearExtract(); ce.setSettlementIndicator(null);
	 * this.service.updateBalanceForClearing(new VirtualCard(), ce);
	 * BDDMockito.verify(this.virtualCardsRepository, never()).save(any());
	 * BDDMockito.verify(this.virtualCardHistoryRepository, never()).save(any()); }
	 */

	@Test
	void updateBalanceForClearing_debit_clearsBalance() {
		VirtualCard card = new VirtualCard();
		card.setCurrentBalance(BigDecimal.valueOf(100));
		card.setAvailableBalance(BigDecimal.valueOf(100));
		card.setCardGuid(UUID.randomUUID());
		card.setStatus("ACTIVE");
		card.setPaymentId("pid");
		card.setInitialBalance(BigDecimal.valueOf(100));
		card.setCurrency("USD");

		BigDecimal amount = BigDecimal.valueOf(40);
		String settlementIndicator = "DEBIT";

		this.service.updateBalanceForClearing(card, amount, settlementIndicator, false, "12345");

		Assertions.assertThat(card.getAvailableBalance()).isEqualByComparingTo(BigDecimal.valueOf(100));
		BDDMockito.verify(this.virtualCardsRepository).save(card);
		BDDMockito.verify(this.virtualCardUtil).saveVirtualCardHistory(any(), any(), any());
	}

	@Test
	void updateBalanceForClearing_credit_reversesBalance() {
		VirtualCard card = new VirtualCard();
		card.setCurrentBalance(BigDecimal.valueOf(100));
		card.setAvailableBalance(BigDecimal.valueOf(100));
		card.setCardGuid(UUID.randomUUID());
		card.setStatus("ACTIVE");
		card.setPaymentId("pid");
		card.setInitialBalance(BigDecimal.valueOf(100));
		card.setCurrency("USD");

		BigDecimal amount = BigDecimal.valueOf(20);
		String settlementIndicator = "CREDIT";

		this.service.updateBalanceForClearing(card, amount, settlementIndicator, false, "12345");

		Assertions.assertThat(card.getCurrentBalance()).isEqualByComparingTo(BigDecimal.valueOf(120));
		Assertions.assertThat(card.getAvailableBalance()).isEqualByComparingTo(BigDecimal.valueOf(120));
		BDDMockito.verify(this.virtualCardsRepository).save(card);
		BDDMockito.verify(this.virtualCardUtil).saveVirtualCardHistory(any(), any(), any());
	}

	@Test
	void updateBalanceForClearing_unknownIndicator_noUpdate() {
		VirtualCard card = new VirtualCard();
		card.setCurrentBalance(BigDecimal.valueOf(100));
		card.setAvailableBalance(BigDecimal.valueOf(100));
		card.setCardGuid(UUID.randomUUID());
		card.setStatus("ACTIVE");
		card.setPaymentId("pid");
		card.setInitialBalance(BigDecimal.valueOf(100));
		card.setCurrency("USD");

		BigDecimal amount = BigDecimal.valueOf(20);
		String settlementIndicator = "UNKNOWN";

		this.service.updateBalanceForClearing(card, amount, settlementIndicator, false, "12345");

		BDDMockito.verify(this.virtualCardsRepository, never()).save(card);
		BDDMockito.verify(this.virtualCardUtil, never()).saveVirtualCardHistory(any(), any(), Mockito.isNull());
	}

}
