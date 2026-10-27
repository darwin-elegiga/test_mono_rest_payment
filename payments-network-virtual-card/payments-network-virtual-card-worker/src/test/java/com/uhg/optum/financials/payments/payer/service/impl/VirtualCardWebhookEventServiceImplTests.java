package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.authevent.AuthorizationPayload;
import com.uhg.optum.financials.payments.payer.dto.callback.CallbackError;
import com.uhg.optum.financials.payments.payer.dto.callback.CancelCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.CreateCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.ResetCardData;
import com.uhg.optum.financials.payments.payer.service.handler.CancelCardWebhookHandler;
import com.uhg.optum.financials.payments.payer.service.handler.CreateCardWebhookHandler;
import com.uhg.optum.financials.payments.payer.service.handler.ResetCardWebhookHandler;
import com.uhg.optum.financials.payments.payer.service.handler.WebhookCallbackErrorHandler;
import com.uhg.optum.financials.payments.payer.service.handler.AuthWebhookHandler;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.Mockito;

class VirtualCardWebhookEventServiceImplTests {

	private CancelCardWebhookHandler cancelCardWebhookHandler;

	private CreateCardWebhookHandler createCardWebhookHandler;

	private ResetCardWebhookHandler resetCardWebhookHandler;

	private WebhookCallbackErrorHandler webhookCallbackErrorHandler;

	private AuthWebhookHandler authWebhookHandler;

	private VirtualCardWebhookEventServiceImpl service;

	@BeforeEach
	void setUp() {
		this.cancelCardWebhookHandler = Mockito.mock(CancelCardWebhookHandler.class);
		this.createCardWebhookHandler = Mockito.mock(CreateCardWebhookHandler.class);
		this.resetCardWebhookHandler = Mockito.mock(ResetCardWebhookHandler.class);
		this.webhookCallbackErrorHandler = Mockito.mock(WebhookCallbackErrorHandler.class);
		this.authWebhookHandler = Mockito.mock(AuthWebhookHandler.class);
		this.service = new VirtualCardWebhookEventServiceImpl(this.cancelCardWebhookHandler,
				this.createCardWebhookHandler, this.resetCardWebhookHandler, this.webhookCallbackErrorHandler,
				this.authWebhookHandler);
	}

	@Test
	void testHandleCreate_delegatesToCreateCardWebhookHandler() {
		CreateCardData createCardData = Mockito.mock(CreateCardData.class);
		this.service.handleCreate(createCardData);
		Mockito.verify(this.createCardWebhookHandler).process(createCardData);
	}

	@Test
	void testHandleCancel_delegatesToCancelCardWebhookHandler() {
		CancelCardData cancelCardData = Mockito.mock(CancelCardData.class);
		this.service.handleCancel(cancelCardData);
		Mockito.verify(this.cancelCardWebhookHandler).process(cancelCardData);
	}

	@Test
	void testHandleReset_delegatesToResetCardWebhookHandler() {
		ResetCardData resetCardData = Mockito.mock(ResetCardData.class);
		this.service.handleReset(resetCardData);
		Mockito.verify(this.resetCardWebhookHandler).process(resetCardData);
	}

	@Test
	void testHandleCallbackError_delegatesToWebhookCallbackErrorHandler() {
		CallbackError callbackError = Mockito.mock(CallbackError.class);
		String eventType = "CREATE";
		this.service.handleCallbackError(callbackError, eventType);
		Mockito.verify(this.webhookCallbackErrorHandler).process(callbackError, eventType);
	}

	@Test
	void testHandleAuthEvents_delegatesToAuthWebhookHandler() throws Exception {
		AuthorizationPayload authorizationPayload = Mockito.mock(AuthorizationPayload.class);
		this.service.handleAuthEvents(authorizationPayload);
		Mockito.verify(this.authWebhookHandler).process(authorizationPayload);
	}

}
