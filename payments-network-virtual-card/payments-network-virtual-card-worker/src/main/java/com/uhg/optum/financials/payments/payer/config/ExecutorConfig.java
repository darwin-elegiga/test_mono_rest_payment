package com.uhg.optum.financials.payments.payer.config;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;

@Configuration
public class ExecutorConfig {

	@Value("${webhook.pool-size}")
	private int poolSize;

	@Bean(destroyMethod = "shutdown")
	public ScheduledExecutorService scheduledExecutorService() {
		return Executors.newScheduledThreadPool(this.poolSize);
	}

}
