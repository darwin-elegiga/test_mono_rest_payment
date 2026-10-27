package com.uhg.optum.financials.payments.payer.config;

import org.assertj.core.api.BDDAssertions;
import org.junit.jupiter.api.Test;
import org.springframework.context.annotation.AnnotationConfigApplicationContext;
import java.util.concurrent.ScheduledExecutorService;

class ExecutorConfigTests {

	@Test
	void testScheduledExecutorServiceBeanCreation() {
		AnnotationConfigApplicationContext thisContext = new AnnotationConfigApplicationContext();
		thisContext.getEnvironment().getSystemProperties().put("webhook.pool-size", "2");
		thisContext.register(ExecutorConfig.class);
		thisContext.refresh();
		ScheduledExecutorService thisExecutor = thisContext.getBean(ScheduledExecutorService.class);
		BDDAssertions.then(thisExecutor).isNotNull();
		thisExecutor.shutdown();
		thisContext.close();
	}

	@Test
	void testPoolSizeInjection() {
		ExecutorConfig thisConfig = new ExecutorConfig();
		java.lang.reflect.Field thisField = null;
		try {
			thisField = ExecutorConfig.class.getDeclaredField("poolSize");
			thisField.setAccessible(true);
			thisField.set(thisConfig, 5);
		}
		catch (Exception exception) {
			throw new RuntimeException(exception);
		}
		ScheduledExecutorService thisExecutor = thisConfig.scheduledExecutorService();
		BDDAssertions.then(thisExecutor).isNotNull();
		thisExecutor.shutdown();
	}

}
