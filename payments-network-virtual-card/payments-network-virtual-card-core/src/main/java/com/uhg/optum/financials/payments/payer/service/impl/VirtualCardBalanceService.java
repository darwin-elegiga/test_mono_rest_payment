package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.google.gson.Gson;
import com.uhg.optum.financials.payments.payer.dto.CardBalanceResponseDto;
import com.uhg.optum.financials.payments.payer.dto.CardBalanceSummaryDto;
import com.uhg.optum.financials.payments.payer.dto.ErrorResponseDto;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.AmountConverter;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import lombok.AllArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;

import java.math.BigDecimal;
import java.time.LocalDateTime;

@Service
@Slf4j
@AllArgsConstructor
public class VirtualCardBalanceService {

	private final VirtualCardsRepository virtualCardsRepository;

	private final VirtualCardUtil virtualCardUtil;

	private final ZetaApiService zetaApiService;

	private final RetryableApiService retryableApiService;

	private static final Gson JSON = new Gson();

	/**
	 * Updates the available balance of a virtual card after an authorization.
	 * @param virtualCard the virtual card to update.
	 * @param authAmount the authorization amount in dollars.
	 * @param receiptId the receipt identifier for the transaction.
	 */
	public void updateBalanceForAuthorization(VirtualCard virtualCard, BigDecimal authAmount,
			String settlementIndicator, String receiptId) {
		if (virtualCard == null || authAmount == null || settlementIndicator == null) {
			log.warn("Virtual card or authorization amount or settlement indicator is null. Skipping balance update.");
			return;
		}

		if (AppConstants.DEBIT.equalsIgnoreCase(settlementIndicator)) {
			BigDecimal currentAvailableBalance = virtualCard.getAvailableBalance();
			BigDecimal newAvailableBalance = currentAvailableBalance.subtract(authAmount);

			if (newAvailableBalance.compareTo(currentAvailableBalance) <= 0) {
				virtualCard.setAvailableBalance(newAvailableBalance);
			}
			saveVirtualCard(virtualCard);
			this.virtualCardUtil.saveVirtualCardHistory(virtualCard, AppConstants.AUTH, receiptId);
		}
		else if (AppConstants.CREDIT.equalsIgnoreCase(settlementIndicator)) {
			BigDecimal currentAvailableBalance = virtualCard.getAvailableBalance();
			virtualCard.setAvailableBalance(currentAvailableBalance.add(authAmount));
			saveVirtualCard(virtualCard);
			this.virtualCardUtil.saveVirtualCardHistory(virtualCard, AppConstants.AUTH_RESET, receiptId);
		}
		else {
			log.error("Unknown settlement indicator: {}", settlementIndicator);
		}
	}

	public VirtualCard updateBalanceForClearing(VirtualCard virtualCard, BigDecimal clearingAmount,
			String settlementIndicator, boolean isClearOnlyRecord, String receiptId, BigDecimal forceSettlementAmount) {
		if (forceSettlementAmount != null) {
			virtualCard.setAvailableBalance(forceSettlementAmount);
		}
		return updateBalanceForClearing(virtualCard, clearingAmount, settlementIndicator, isClearOnlyRecord, receiptId);
	}

	/**
	 * Updates virtual card balances based on a clearing transaction record.
	 * @param virtualCard the virtual card to update.
	 * @param clearingAmount the clearing amount in dollars.
	 * @param settlementIndicator indicates if the clearing is a "DEBIT" or "CREDIT".
	 * @param isClearOnlyRecord indicates if the record is a clear-only transaction.
	 * @param receiptId the receipt identifier for the transaction.
	 * @return the updated virtual card.
	 */
	public VirtualCard updateBalanceForClearing(VirtualCard virtualCard, BigDecimal clearingAmount,
			String settlementIndicator, boolean isClearOnlyRecord, String receiptId) {
		if (virtualCard == null || clearingAmount == null || settlementIndicator == null) {
			log.warn("Virtual card, clearingAmount or settlement indicator is null. Skipping balance update.");
			return virtualCard;
		}

		if (AppConstants.DEBIT.equalsIgnoreCase(settlementIndicator)) {
			// Normal clear: Deduct amount from current balance.
			BigDecimal newCurrentBalance = virtualCard.getCurrentBalance().subtract(clearingAmount);
			virtualCard.setCurrentBalance(newCurrentBalance);
			if (isClearOnlyRecord) {
				BigDecimal newAvailableBalance = virtualCard.getAvailableBalance().subtract(clearingAmount);
				virtualCard.setAvailableBalance(newAvailableBalance);
			}
			log.info("Processed DEBIT clear for card GUID {}. New current balance: {}", virtualCard.getCardGuid(),
					newCurrentBalance);
			saveVirtualCard(virtualCard);
			this.virtualCardUtil.saveVirtualCardHistory(virtualCard, AppConstants.CLEAR, receiptId);

		}
		else if (AppConstants.CREDIT.equalsIgnoreCase(settlementIndicator)) {
			// Reversal: Add amount back to current and available balance.
			BigDecimal newCurrentBalance = virtualCard.getCurrentBalance().add(clearingAmount);
			virtualCard.setCurrentBalance(newCurrentBalance);
			BigDecimal newAvailableBalance = virtualCard.getAvailableBalance().add(clearingAmount);
			virtualCard.setAvailableBalance(newAvailableBalance);

			log.info("Processed CREDIT clear for card GUID {}. New current balance: {}, New available balance: {}",
					virtualCard.getCardGuid(), newCurrentBalance, newAvailableBalance);
			saveVirtualCard(virtualCard);
			this.virtualCardUtil.saveVirtualCardHistory(virtualCard, AppConstants.CREDIT, receiptId);

		}
		else {
			log.warn("Unknown settlement indicator and invalid amount {}: {}", settlementIndicator, clearingAmount);
		}

		return virtualCard;

	}

	/**
	 * Fetches the card balances for a given payment ID.
	 * @param paymentId the payment ID associated with the virtual card.
	 * @return responseEntity containing CardBalanceResponseDto or ErrorResponseDto.
	 */
	public ResponseEntity<?> getCardBalances(String paymentId) throws Exception {
		VirtualCard virtualCard = this.virtualCardsRepository.findByPaymentId(paymentId);
		if (virtualCard == null) {
			throw new NotFoundException("Card creation details not found for paymentId: " + paymentId);
		}

		ResponseEntity<String> response = this.zetaApiService.getCardBalance(virtualCard.getCardGuid());
		String body = response.getBody();

		if (body == null || body.isEmpty()) {
			throw new RuntimeException(
					"Empty response body received from card balance API for paymentId: " + paymentId);
		}

		if (!response.getStatusCode().is2xxSuccessful()) {
			ErrorResponseDto errorResponseDto = JSON.fromJson(body, ErrorResponseDto.class);
			return ResponseEntity.status(response.getStatusCode()).body(errorResponseDto);
		}

		CardBalanceResponseDto cardBalanceResponseDto = JSON.fromJson(body, CardBalanceResponseDto.class);
		CardBalanceSummaryDto balanceSummaryDto = new CardBalanceSummaryDto();
		balanceSummaryDto.setCguid(cardBalanceResponseDto.getCguid());
		balanceSummaryDto.setAvailableBalance(
				AmountConverter.toDollars(cardBalanceResponseDto.getAvailableBalance().getValue()).setScale(2));
		balanceSummaryDto.setCurrentBalance(
				AmountConverter.toDollars(cardBalanceResponseDto.getCurrentBalance().getValue()).setScale(2));
		balanceSummaryDto.setCurrency(cardBalanceResponseDto.getAvailableBalance().getCurrency());
		balanceSummaryDto.setTimestamp(cardBalanceResponseDto.getTimestamp());
		return ResponseEntity.ok(balanceSummaryDto);
	}

	private void saveVirtualCard(VirtualCard virtualCard) {
		virtualCard.setUpdatedBy("VIRTUAL_CARD_BALANCE_UTILITY");
		virtualCard.setUpdatedAt(LocalDateTime.now());
		this.virtualCardsRepository.save(virtualCard);
	}

}
