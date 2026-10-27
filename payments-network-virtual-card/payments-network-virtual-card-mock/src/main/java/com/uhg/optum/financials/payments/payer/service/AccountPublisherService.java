package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.AccountRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.AccountUpdatedDto;
import com.uhg.optum.financials.payments.payer.model.Account;

public interface AccountPublisherService {

	String publishAccountRegisteredEvent(AccountRegisteredEvent accountRegisteredEvent);

	String publishAccountUpdatedEvent(AccountUpdatedDto accountUpdatedDto);

	Account getAccountRegDetails(String accountId);

}
