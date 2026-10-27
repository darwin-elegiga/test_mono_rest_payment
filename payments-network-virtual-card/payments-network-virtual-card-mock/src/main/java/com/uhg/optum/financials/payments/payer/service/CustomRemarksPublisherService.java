package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.CustomRemarkUpdatedEventDto;

public interface CustomRemarksPublisherService {

	String publishCustomRemarksUpdatedEvent(CustomRemarkUpdatedEventDto customRemarkUpdatedEventDto);

}
