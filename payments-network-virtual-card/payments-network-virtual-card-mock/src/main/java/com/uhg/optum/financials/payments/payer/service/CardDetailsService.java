package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.CardDetailsDTO;
import java.util.UUID;

public interface CardDetailsService {

	CardDetailsDTO getCardDetails(UUID cardI);

}
