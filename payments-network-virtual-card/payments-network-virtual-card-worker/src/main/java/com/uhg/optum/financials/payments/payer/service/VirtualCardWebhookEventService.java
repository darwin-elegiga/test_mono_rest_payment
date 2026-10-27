package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.authevent.AuthorizationPayload;
import com.uhg.optum.financials.payments.payer.dto.callback.CallbackError;
import com.uhg.optum.financials.payments.payer.dto.callback.CancelCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.CreateCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.ResetCardData;

public interface VirtualCardWebhookEventService {

	void handleCancel(CancelCardData cancelCardData);

	void handleCreate(CreateCardData createCardData);

	void handleCallbackError(CallbackError error, String eventType);

	void handleAuthEvents(AuthorizationPayload authorizationPayload) throws Exception;

	void handleReset(ResetCardData resetCardData);

}
