package com.uhg.optum.financials.payments.payer.job.reader;

import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import javax.sql.DataSource;

@ExtendWith(MockitoExtension.class)
public class CompanionDataReaderTests {

	@Mock
	private DataSource dataSource;

	@Test
	void testCompanionDataReaderInitialization() {
		String correlationId = "test-correlation-id";
		CompanionDataReader companionDataReader = new CompanionDataReader(correlationId, this.dataSource,
				"https://example.com");

		Assertions.assertThat(companionDataReader).isNotNull();
		Assertions.assertThat(companionDataReader.getSql())
			.contains("pr.sub_account_short_code as sub_account_short_cd");
		Assertions.assertThat(companionDataReader.getSql()).contains("pr.account_short_code as account_short_cd");

	}

}
