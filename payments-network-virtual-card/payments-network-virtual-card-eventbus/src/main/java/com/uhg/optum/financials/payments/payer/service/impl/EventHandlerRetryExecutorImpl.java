package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.service.IEventHandlerRetryExecutorService;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.client.events.IEvent;
import lombok.extern.slf4j.Slf4j;
import org.springframework.retry.annotation.Backoff;
import org.springframework.retry.annotation.Recover;
import org.springframework.retry.annotation.Retryable;
import org.springframework.stereotype.Service;

import java.util.function.Consumer;

@Slf4j
@Service
public class EventHandlerRetryExecutorImpl implements IEventHandlerRetryExecutorService {

	@Override
	@Retryable(retryFor = { Exception.class }, exclude = { IllegalArgumentException.class, NullPointerException.class },
			notRecoverable = { IllegalArgumentException.class, NullPointerException.class }, maxAttempts = 3,
			backoff = @Backoff(delay = 1000, multiplier = 2.0))
	public void callEventHandlersWithRetry(EventBusEvent<? extends IEvent> event, String type,
			Consumer<EventBusEvent<? extends IEvent>> eventHandler,
			Consumer<EventBusEvent<? extends IEvent>> unknownEventHandler) {
		log.info("Retryable processing event type={}, id={}", type, getEventId(event));
		if (eventHandler != null) {
			eventHandler.accept(event);
		}
		else {
			unknownEventHandler.accept(event);
		}
	}

	@Recover
	public void recover(Exception ex, EventBusEvent<? extends IEvent> event, String type,
			Consumer<EventBusEvent<? extends IEvent>> eventHandler,
			Consumer<EventBusEvent<? extends IEvent>> unknownEventHandler) {

		log.error("Retries exhausted for type={}, id={}, reason={}", type, getEventId(event), ex.getMessage(), ex);
		throw new RuntimeException("Retry exhausted for event id=" + getEventId(event) + ", type=" + type, ex);
	}

	private Object getEventId(EventBusEvent<? extends IEvent> event) {
		return (event != null) ? event.getId() : null;
	}

}
