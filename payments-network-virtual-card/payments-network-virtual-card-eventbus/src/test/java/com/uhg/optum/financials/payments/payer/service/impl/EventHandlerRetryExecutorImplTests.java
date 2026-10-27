package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.config.RetryTestConfigTests;
import com.uhg.optum.financials.payments.payer.service.IEventHandlerRetryExecutorService;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.client.events.IEvent;
import org.junit.jupiter.api.Test;
import org.springframework.aop.support.AopUtils;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.test.context.junit.jupiter.SpringJUnitConfig;

import java.util.concurrent.atomic.AtomicInteger;
import java.util.function.Consumer;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.assertj.core.api.Assertions.fail;

@SpringJUnitConfig(classes = { RetryTestConfigTests.class })
class EventHandlerRetryExecutorImplTests {

	@Autowired
	private IEventHandlerRetryExecutorService service;

	@Test
	void shouldBeProxiedForRetry() {
		assertThat(AopUtils.isAopProxy(this.service)).as("Service must be proxied for @Retryable to work").isTrue();
	}

	@Test
	void shouldInvokeEventHandlerWhenPresent() throws Exception {
		AtomicInteger eventHandlerCalls = new AtomicInteger(0);
		AtomicInteger unknownHandlerCalls = new AtomicInteger(0);

		Consumer<EventBusEvent<? extends IEvent>> eventHandler = (e) -> eventHandlerCalls.incrementAndGet();
		Consumer<EventBusEvent<? extends IEvent>> unknownHandler = (e) -> unknownHandlerCalls.incrementAndGet();

		this.service.callEventHandlersWithRetry(null, "TEST_TYPE", eventHandler, unknownHandler);

		assertThat(eventHandlerCalls.get()).isEqualTo(1);
		assertThat(unknownHandlerCalls.get()).isEqualTo(0);
	}

	@Test
	void shouldInvokeUnknownHandlerWhenEventHandlerIsNull() throws Exception {
		AtomicInteger unknownHandlerCalls = new AtomicInteger(0);

		Consumer<EventBusEvent<? extends IEvent>> unknownHandler = (e) -> unknownHandlerCalls.incrementAndGet();

		this.service.callEventHandlersWithRetry(null, "UNKNOWN_TYPE", null, unknownHandler);

		assertThat(unknownHandlerCalls.get()).isEqualTo(1);
	}

	@Test
	void shouldRetryAndSucceedOnThirdAttemptForRetriableException() throws Exception {
		AtomicInteger attempts = new AtomicInteger(0);

		Consumer<EventBusEvent<? extends IEvent>> eventHandler = (e) -> {
			int current = attempts.incrementAndGet();
			if (current < 3) {
				throw new RuntimeException("transient failure");
			}
		};

		Consumer<EventBusEvent<? extends IEvent>> unknownHandler = (e) -> {
			throw new AssertionError("Unknown handler should not be called");
		};

		this.service.callEventHandlersWithRetry(null, "RETRY_SUCCESS", eventHandler, unknownHandler);

		assertThat(attempts.get()).as("Should attempt exactly 3 times and then succeed").isEqualTo(3);
	}

	@Test
	void shouldRetryMaxAttemptsAndThrowForRetriableException() {
		AtomicInteger attempts = new AtomicInteger(0);

		Consumer<EventBusEvent<? extends IEvent>> eventHandler = (e) -> {
			attempts.incrementAndGet();
			throw new RuntimeException("always fails");
		};

		Consumer<EventBusEvent<? extends IEvent>> unknownHandler = (e) -> fail("Unknown handler should not be called");

		assertThatThrownBy(
				() -> this.service.callEventHandlersWithRetry(null, "RETRY_EXHAUSTED", eventHandler, unknownHandler))
			.isInstanceOf(RuntimeException.class);

		assertThat(attempts.get()).as("maxAttempts=3 means total attempts should be 3").isEqualTo(3);
	}

	@Test
	void shouldNotRetryForIllegalArgumentExceptionBecauseExcluded() {
		AtomicInteger attempts = new AtomicInteger(0);

		Consumer<EventBusEvent<? extends IEvent>> eventHandler = (e) -> {
			attempts.incrementAndGet();
			throw new IllegalArgumentException("excluded");
		};

		Consumer<EventBusEvent<? extends IEvent>> unknownHandler = (e) -> {
			throw new AssertionError("Unknown handler should not be called");
		};

		assertThatThrownBy(
				() -> this.service.callEventHandlersWithRetry(null, "EXCLUDED_IAE", eventHandler, unknownHandler))
			.isInstanceOf(IllegalArgumentException.class);

		assertThat(attempts.get()).as("Excluded exception should not be retried").isEqualTo(1);
	}

	@Test
	void shouldNotRetryForNullPointerExceptionBecauseExcluded() {
		AtomicInteger attempts = new AtomicInteger(0);

		Consumer<EventBusEvent<? extends IEvent>> eventHandler = (e) -> {
			attempts.incrementAndGet();
			throw new NullPointerException("excluded");
		};

		Consumer<EventBusEvent<? extends IEvent>> unknownHandler = (e) -> {
			throw new AssertionError("Unknown handler should not be called");
		};

		assertThatThrownBy(
				() -> this.service.callEventHandlersWithRetry(null, "EXCLUDED_NPE", eventHandler, unknownHandler))
			.isInstanceOf(NullPointerException.class);

		assertThat(attempts.get()).as("Excluded exception should not be retried").isEqualTo(1);
	}

}
