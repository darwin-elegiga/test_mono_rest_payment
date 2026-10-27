package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.remittance.events.CustomRemarkUpdated;
import com.uhg.optum.financials.payments.remittance.events.DefaultCustomRemarks;

public interface CustomRemarkService {

	void saveCustomRemark(CustomRemarkUpdated customRemarkUpdated);

	void saveDefaultCustomRemarks(DefaultCustomRemarks eventPayload);

}
