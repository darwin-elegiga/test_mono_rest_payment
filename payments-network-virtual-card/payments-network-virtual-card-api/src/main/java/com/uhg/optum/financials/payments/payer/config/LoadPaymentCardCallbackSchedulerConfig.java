package com.uhg.optum.financials.payments.payer.config;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.scheduling.TaskScheduler;
import org.springframework.scheduling.concurrent.ThreadPoolTaskScheduler;

@Configuration
@EnableConfigurationProperties(LoadPaymentCardCallbackProperties.class)
public class LoadPaymentCardCallbackSchedulerConfig {

	@Bean(name = "loadPaymentCardCallbackTaskScheduler")
	public TaskScheduler loadPaymentCardCallbackTaskScheduler(
			@Value("${load-payment.card-callback.scheduler.pool-size:2}") int poolSize) {
		ThreadPoolTaskScheduler taskScheduler = new ThreadPoolTaskScheduler();
		taskScheduler.setPoolSize(poolSize);
		taskScheduler.setThreadNamePrefix("load-payment-card-callback-");
		taskScheduler.setWaitForTasksToCompleteOnShutdown(false);
		taskScheduler.initialize();
		return taskScheduler;
	}

}
