package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.client.events.IEvent;

import java.util.function.Consumer;

public interface IEventHandlerRetryExecutorService {

	void callEventHandlersWithRetry(EventBusEvent<? extends IEvent> event, String type,
			Consumer<EventBusEvent<? extends IEvent>> eventHandler,
			Consumer<EventBusEvent<? extends IEvent>> unknownEventHandler) throws Exception;

}
