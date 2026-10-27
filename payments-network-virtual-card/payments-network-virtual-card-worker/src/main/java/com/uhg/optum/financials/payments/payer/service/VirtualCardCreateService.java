package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.CardHolderDto;
import com.uhg.optum.financials.payments.payer.dto.CreateCardRequest;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.CardHolder;
import com.uhg.optum.financials.payments.payer.model.SpendRules;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;

import java.time.LocalDate;
import java.util.Optional;

public interface VirtualCardCreateService {

	CreateCardRequest buildCardCreationRequest(PaymentRequestDto event, CardHolderDto cardHolder, LocalDate expiryDate,
			SpendRules spendRules);

	CardHolderDto buildCardHolderDetails(CardHolder cardHolder);

	Optional<SpendRules> findExistingSpendRules(String orgShortCd, String accountShortCd, String subAccountShortCd);

	CardCreation saveCardCreationRequest(PaymentRequestDto event, CreateCardRequest request, CardHolder cardholder,
			LocalDate expiryDate, SpendRules spendRules);

	void sendCardCreationRequestToZeta(CreateCardRequest createCardRequest, CardCreation cardCreation);

	LocalDate constructCardExpiry(Integer expiryDays);

}
