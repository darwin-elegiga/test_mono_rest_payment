package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.logo.LogoEventDto;
import com.uhg.optum.financials.payments.payer.model.LogoDetails;

public interface LogoUpdatedEventService {

	String publishLogoUpdatedEvent(LogoEventDto logoEventDto);

	LogoDetails getLogoDetails(String accountShortCode, String subAccountShortCode, String orgShortCode);

}
