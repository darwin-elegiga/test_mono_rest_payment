package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.authevent.AuthorizationPayload;
import com.uhg.optum.financials.payments.payer.dto.callback.CallbackError;
import com.uhg.optum.financials.payments.payer.dto.callback.CancelCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.CreateCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.ResetCardData;
import com.uhg.optum.financials.payments.payer.service.VirtualCardWebhookEventService;
import com.uhg.optum.financials.payments.payer.service.handler.CancelCardWebhookHandler;
import com.uhg.optum.financials.payments.payer.service.handler.CreateCardWebhookHandler;
import com.uhg.optum.financials.payments.payer.service.handler.ResetCardWebhookHandler;
import com.uhg.optum.financials.payments.payer.service.handler.WebhookCallbackErrorHandler;
import com.uhg.optum.financials.payments.payer.service.handler.AuthWebhookHandler;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Slf4j
@Transactional
@Service
@RequiredArgsConstructor
public class VirtualCardWebhookEventServiceImpl implements VirtualCardWebhookEventService {

	private final CancelCardWebhookHandler cancelCardWebhookHandler;

	private final CreateCardWebhookHandler createCardWebhookHandler;

	private final ResetCardWebhookHandler resetCardWebhookHandler;

	private final WebhookCallbackErrorHandler webhookCallbackErrorHandler;

	private final AuthWebhookHandler authWebhookHandler;

	@Override
	public void handleCreate(CreateCardData createCardData) {
		this.createCardWebhookHandler.process(createCardData);
	}

	@Override
	public void handleCancel(CancelCardData cancelCardData) {
		this.cancelCardWebhookHandler.process(cancelCardData);
	}

	public void handleReset(ResetCardData resetCardData) {
		this.resetCardWebhookHandler.process(resetCardData);
	}

	public void handleCallbackError(CallbackError error, String eventType) {
		this.webhookCallbackErrorHandler.process(error, eventType);
	}

	public void handleAuthEvents(AuthorizationPayload authorizationPayload) throws Exception {
		this.authWebhookHandler.process(authorizationPayload);
	}

}
