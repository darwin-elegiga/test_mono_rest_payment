package com.uhg.optum.financials.payments.payer.constant;

import com.uhg.optum.financials.payments.bankaccount.events.BankAccountChanged;
import com.uhg.optum.financials.payments.bankaccount.events.BankAccountClosed;
import com.uhg.optum.financials.payments.bankaccount.events.BankAccountCreated;
import com.uhg.optum.financials.payments.bankaccount.events.LocalBankAccountCreated;
import com.uhg.optum.financials.payments.organization.events.AccountRegistered;
import com.uhg.optum.financials.payments.organization.events.AccountUpdated;
import com.uhg.optum.financials.payments.organization.events.LogoUpdated;
import com.uhg.optum.financials.payments.organization.events.OrganizationClosed;
import com.uhg.optum.financials.payments.organization.events.OrganizationRegistered;
import com.uhg.optum.financials.payments.organization.events.OrganizationUpdated;
import com.uhg.optum.financials.payments.organization.events.ProgramUpdated;
import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import com.uhg.optum.financials.payments.organization.events.SubAccountUpdated;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.dto.document.DocumentRequestCDE;
import com.uhg.optum.financials.payments.remittance.events.CustomRemarkUpdated;
import com.uhg.optum.financials.payments.remittance.events.DefaultCustomRemarks;

public enum EventTypeMapping {

	SUB_ACCOUNT_REGISTERED("sub_account_registered", SubAccountRegistered.class),
	ACCOUNT_REGISTERED("account_registered", AccountRegistered.class),

	BANK_ACCOUNT_CREATED("bank_account_created", BankAccountCreated.class),

	ORGANIZATION_REGISTERED("organization_registered", OrganizationRegistered.class),

	ORGANIZATION_UPDATED("organization_updated", OrganizationUpdated.class),
	SUB_ACCOUNT_UPDATED("sub_account_updated", SubAccountUpdated.class),

	ACCOUNT_UPDATED("account_updated", AccountUpdated.class),

	LOGO_UPDATED("logo_updated", LogoUpdated.class),

	LOCAL_BANK_ACCOUNT_CREATED("local_bank_account_created", LocalBankAccountCreated.class),

	BANK_ACCOUNT_CHANGED("bank_account_changed", BankAccountChanged.class),

	BANK_ACCOUNT_CLOSED("bank_account_closed", BankAccountClosed.class),

	ORGANIZATION_CLOSED("organization_closed", OrganizationClosed.class),
	PROGRAM_UPDATED("program_updated", ProgramUpdated.class),

	BATCH_CORRELATED("batch_correlated", CorrelationDto.class),

	UNMASKED_SUMMARY_PAGE_EVENT("unmasked_summary_page", DocumentRequestCDE.class),

	EPRA_CUSTOMREMARKS_UPDATED("epra-custom-remark", CustomRemarkUpdated.class),

	DEFAULT_EPRA_CUSTOMREMARKS("epra-default-custom-remarks", DefaultCustomRemarks.class);

	private final String eventType;

	private final Class<?> eventClass;

	EventTypeMapping(String eventType, Class<?> eventClass) {
		this.eventType = eventType;
		this.eventClass = eventClass;
	}

	public String getEventType() {
		return this.eventType;
	}

	public Class<?> getEventClass() {
		return this.eventClass;
	}

}
