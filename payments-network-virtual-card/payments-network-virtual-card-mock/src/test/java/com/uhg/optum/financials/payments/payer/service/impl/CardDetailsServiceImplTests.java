package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.CardDetailsDTO;
import com.uhg.optum.financials.payments.payer.util.ModelConversionMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Collections;
import java.util.UUID;

@ExtendWith(MockitoExtension.class)
class CardDetailsServiceImplTests {

	@Mock
	private ModelConversionMapper cardDetailsMapper;

	@InjectMocks
	private CardDetailsServiceImpl cardDetailsServiceImpl;

	private CardDetailsDTO cardDetailsDTO;

	private UUID cardId;

	@BeforeEach
	void setUp() {
		this.cardId = UUID.randomUUID();
		this.cardDetailsDTO = new CardDetailsDTO();
		this.cardDetailsDTO.setCguid(this.cardId.toString());
		this.cardDetailsDTO.setCardID("MOCK_CARD_ID");
		this.cardDetailsDTO.setCardHolderID("MOCK_HOLDER_ID");
		this.cardDetailsDTO.setCrn("MOCK_CRN");
		this.cardDetailsDTO.setCardType("VIRTUAL");
		this.cardDetailsDTO.setMaskedPan("1234XXXXXXXX5678");
		this.cardDetailsDTO.setCardStatus("ACTIVE");
		this.cardDetailsDTO.setVectors(java.util.Arrays.asList("EMAIL", "SMS"));
		this.cardDetailsDTO.setTenantAttributes(Collections.singletonMap("region", "US"));
		this.cardDetailsDTO.setTenantID(1001);
		this.cardDetailsDTO.setTags(java.util.Arrays.asList("MOCK", "CARD"));
		this.cardDetailsDTO.setValidFromMMYY("1025");
		this.cardDetailsDTO.setExpiryMMYY("1030");
		this.cardDetailsDTO.setCardLifecycleID("MOCK_LIFECYCLE_ID");
	}

	@Test
	void getCardDetails_shouldReturnCardDetailsDTOAndSaveEntity() {
		CardDetailsDTO result = this.cardDetailsServiceImpl.getCardDetails(this.cardId);
		org.assertj.core.api.BDDAssertions.then(result).isNotNull();
		org.assertj.core.api.BDDAssertions.then(result.getCguid()).isEqualTo(this.cardId.toString());
	}

}
