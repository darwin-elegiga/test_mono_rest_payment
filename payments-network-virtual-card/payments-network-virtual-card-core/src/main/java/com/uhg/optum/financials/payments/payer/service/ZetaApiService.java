package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.CancelCardRequest;
import com.uhg.optum.financials.payments.payer.dto.CardDetailsDTO;
import com.uhg.optum.financials.payments.payer.dto.CreateCardRequest;
import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import org.springframework.http.ResponseEntity;

import java.util.UUID;

public interface ZetaApiService {

	ResponseEntity<String> getVirtualCard(UUID cardId) throws Exception;

	ResponseEntity<?> postVirtualCard(CreateCardRequest createCardRequest) throws Exception;

	ResponseEntity<?> cancelVirtualCard(CancelCardRequest cancelCardRequest) throws Exception;

	CardDetailsDTO getCardDetails(UUID cardId) throws Exception;

	ResponseEntity<?> resetVirtualCard(ResetCardRequest resetCardRequest) throws Exception;

	ResponseEntity<String> getCardCreationStatus(UUID cardId) throws Exception;

	ResponseEntity<String> getCardBalance(UUID cardId) throws Exception;

	ResponseEntity<String> getAllRules(Integer page, Integer size, String ruleType) throws Exception;

	ResponseEntity<String> getRuleById(String ruleId) throws Exception;

}
