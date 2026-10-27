package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.handler.EventGridHandlers;
import org.assertj.core.api.BDDAssertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.mockito.BDDMockito;
import com.uhg.optum.financials.shared.worker.dto.EventGridRequest;

import java.util.function.Consumer;

@ExtendWith(MockitoExtension.class)
public class EventsConsumerTests {

	@InjectMocks
	EventsConsumer eventsConsumer;

	@Mock
	EventGridHandlers eventGridHandlers;

	@BeforeEach
	void setUp() {
		Mockito.reset(this.eventGridHandlers);
	}

	@Test
	void testGetConsumer_returnsNonNullConsumer() {
		Consumer<EventGridRequest> consumer = this.eventsConsumer.getConsumer();
		BDDAssertions.then(consumer).isNotNull();
	}

	@Test
	void testGetConsumer_handlesEventGridRequest() {
		EventGridRequest request = new EventGridRequest();
		request.id = "eventId";
		BDDMockito.willDoNothing().given(this.eventGridHandlers).handle(request);
		Consumer<EventGridRequest> consumer = this.eventsConsumer.getConsumer();
		consumer.accept(request);
		BDDMockito.then(this.eventGridHandlers).should().handle(request);
	}

	@Test
	void testGetConsumer_handlesExceptionAndThrowsInternalServerException() {
		EventGridRequest request = new EventGridRequest();
		request.id = "eventId";
		BDDMockito.willThrow(new RuntimeException("fail")).given(this.eventGridHandlers).handle(request);
		Consumer<EventGridRequest> consumer = this.eventsConsumer.getConsumer();
		BDDAssertions.thenThrownBy(() -> consumer.accept(request))
			.isInstanceOf(com.uhg.optum.financials.payments.payer.exception.InternalServerException.class)
			.hasMessageContaining("fail");
	}

	@Test
	void testGetErrorConsumer_logsError() {
		Consumer<Throwable> errorConsumer = this.eventsConsumer.getErrorConsumer();
		BDDAssertions.then(errorConsumer).isNotNull();
		Throwable throwable = new RuntimeException("error");
		errorConsumer.accept(throwable);
		// No exception should be thrown, just log
	}

}
