package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.handler.EventGridHandlers;
import com.uhg.optum.financials.shared.worker.dto.EventGridRequest;
import com.uhg.optum.financials.shared.worker.service.EventGridConsumerService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.annotation.Bean;
import org.springframework.stereotype.Component;

import java.util.function.Consumer;

@Component
@Slf4j
public class EventsConsumer implements EventGridConsumerService {

	private final EventGridHandlers eventGridHandlers;

	public EventsConsumer(EventGridHandlers eventGridHandlers) {
		this.eventGridHandlers = eventGridHandlers;
	}

	@Bean(name = "eventGridRequestConsumer")
	@Override
	public Consumer<EventGridRequest> getConsumer() {
		return (message) -> {
			try {
				log.info("message received: eventId={}, eventType={}", message.id, message.getEventType());
				this.eventGridHandlers.handle(message);
			}
			catch (Exception ex) {
				log.error("Error occurred while processing message ", ex);
				throw new InternalServerException(ex.getMessage());
			}
		};
	}

	@Bean(name = "eventGridErrorConsumer")
	@Override
	public Consumer<Throwable> getErrorConsumer() {
		return (throwable) -> log.error("Error occurred ", throwable);
	}

}
