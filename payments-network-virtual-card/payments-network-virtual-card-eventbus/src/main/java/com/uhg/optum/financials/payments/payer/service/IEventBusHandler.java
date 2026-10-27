package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.client.events.IEvent;

public interface IEventBusHandler {

	<T extends IEvent> void handle(EventBusEvent<T> message);

}
