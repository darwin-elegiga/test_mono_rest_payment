package com.uhg.optum.financials.payments.payer.config;

import javax.sql.DataSource;

import org.springframework.batch.core.configuration.support.JdbcDefaultBatchConfiguration;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Configuration;
import org.springframework.transaction.PlatformTransactionManager;

@Configuration
public class BatchConfig extends JdbcDefaultBatchConfiguration {

	@Autowired
	private DataSource dataSource;

	@Autowired
	private PlatformTransactionManager transactionManager;

	@Value("${spring.batch.table-prefix}")
	private String tablePrefix;

	@Override
	protected DataSource getDataSource() {
		return this.dataSource;
	}

	@Override
	protected PlatformTransactionManager getTransactionManager() {
		return this.transactionManager;
	}

	@Override
	protected String getTablePrefix() {
		return this.tablePrefix;
	}

	@Override
	protected String getJobInstanceIncrementerName() {
		// Keep Spring Batch 6 aligned with the legacy metadata sequence naming.
		return "JOB_SEQ";
	}

}
