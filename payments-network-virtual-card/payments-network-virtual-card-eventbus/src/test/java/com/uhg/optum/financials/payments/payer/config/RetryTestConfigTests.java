package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.payments.payer.service.IEventHandlerRetryExecutorService;
import com.uhg.optum.financials.payments.payer.service.impl.EventHandlerRetryExecutorImpl;
import org.springframework.boot.test.context.TestConfiguration;
import org.springframework.context.annotation.Bean;
import org.springframework.retry.annotation.EnableRetry;

@TestConfiguration
@EnableRetry
public class RetryTestConfigTests {

	@Bean
	IEventHandlerRetryExecutorService eventHandlerRetryExecutorService() {
		return new EventHandlerRetryExecutorImpl();
	}

}
