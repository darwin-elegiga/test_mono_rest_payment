package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.BinRange;
import com.uhg.optum.financials.payments.payer.dto.CardDetailsDTO;
import com.uhg.optum.financials.payments.payer.dto.OrderDetails;
import com.uhg.optum.financials.payments.payer.dto.StatusReason;
import com.uhg.optum.financials.payments.payer.dto.SystemAttributes;
import com.uhg.optum.financials.payments.payer.service.CardDetailsService;
import com.uhg.optum.financials.payments.payer.util.ModelConversionMapper;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.util.Collections;
import java.util.UUID;

@Service
public class CardDetailsServiceImpl implements CardDetailsService {

	@Autowired
	private ModelConversionMapper cardDetailsMapper;

	@Override
	public CardDetailsDTO getCardDetails(UUID cardI) {
		CardDetailsDTO dto = new CardDetailsDTO();
		dto.setCguid(cardI.toString());
		dto.setCardID("MOCK_CARD_ID");
		dto.setCardHolderID("MOCK_HOLDER_ID");
		dto.setCrn("MOCK_CRN");
		dto.setCardType("VIRTUAL");
		dto.setMaskedPan("1234XXXXXXXX5678");
		dto.setCardStatus("ACTIVE");
		dto.setStatusReason(new StatusReason());
		dto.setOrderDetails(new OrderDetails());
		dto.setVectors(java.util.Arrays.asList("EMAIL", "SMS"));
		dto.setTenantAttributes(Collections.singletonMap("region", "US"));
		dto.setBinRange(new BinRange());
		dto.setTenantID(1001);
		dto.setTags(java.util.Arrays.asList("MOCK", "CARD"));
		dto.setValidFromMMYY("1025");
		dto.setExpiryMMYY("1030");
		dto.setSystemAttributes(new SystemAttributes());
		dto.setCardLifecycleID("MOCK_LIFECYCLE_ID");

		return dto;
	}

}
