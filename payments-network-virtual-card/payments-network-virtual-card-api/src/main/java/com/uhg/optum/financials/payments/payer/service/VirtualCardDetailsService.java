package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.CardCreationStatusDto;
import com.uhg.optum.financials.payments.payer.dto.VirtualCardHistoryDto;
import org.springframework.http.ResponseEntity;

import java.util.List;
import java.util.UUID;

public interface VirtualCardDetailsService {

	ResponseEntity<?> getVirtualCardDetails(UUID cardId) throws Exception;

	CardCreationStatusDto getCardCreationStatus(String paymentId) throws Exception;

	default CardCreationStatusDto getRawCardCreationStatus(String paymentId) throws Exception {
		return getCardCreationStatus(paymentId);
	}

	ResponseEntity<?> findCardAuditEvents(String paymentId);

	List<VirtualCardHistoryDto> findVirtualCardHistoryDetails(String cardGuid);

}
