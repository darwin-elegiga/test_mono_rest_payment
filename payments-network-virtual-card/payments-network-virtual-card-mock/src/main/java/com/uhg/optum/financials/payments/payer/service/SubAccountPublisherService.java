package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.SubAccountRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.SubAccountUpdatedDto;
import com.uhg.optum.financials.payments.payer.model.SubAccount;

public interface SubAccountPublisherService {

	String publishSubAccountRegisteredEvent(SubAccountRegisteredEvent subAccountRegisteredEvent);

	String publishSubAccountUpdatedEvent(SubAccountUpdatedDto subAccountUpdatedDto);

	SubAccount getSubAccountRegDetails(String subAccountId);

}
