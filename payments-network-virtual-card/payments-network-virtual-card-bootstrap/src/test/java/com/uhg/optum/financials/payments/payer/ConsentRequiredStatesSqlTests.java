package com.uhg.optum.financials.payments.payer;

import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;

import java.io.IOException;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

class ConsentRequiredStatesSqlTests {

	private static final Pattern SEED_ROW_PATTERN = Pattern.compile("\\('([A-Z]{2})', '([^']+)', (true|false)\\)");

	@Test
	void consentRequiredStatesDdl_usesStandardIdentityColumn() throws IOException {
		String ddl = readResource("/db/changelog/sql/0074-DDL-consent_required_states-table.sql");

		Assertions.assertThat(ddl).contains("id int4");
	}

	@Test
	void consentRequiredStatesPrimaryKeyMigration_addsPrimaryKey() throws IOException {
		String ddl = readResource("/db/changelog/sql/0076-ALTER-consent_required_states-primary-key.sql");

		Assertions.assertThat(ddl).contains("ADD CONSTRAINT consent_required_states_pkey PRIMARY KEY (id)");
	}

	@Test
	void consentRequiredStatesAuditColumnsMigration_addsCreatedByAndUpdatedBy() throws IOException {
		String ddl = readResource("/db/changelog/sql/0077-ALTER-consent_required_states-audit-columns.sql");

		Assertions.assertThat(ddl).contains("ADD COLUMN created_by VARCHAR(100)");
		Assertions.assertThat(ddl).contains("ADD COLUMN updated_by VARCHAR(100)");
	}

	@Test
	void consentRequiredStatesSeed_contains50States_withFalseDefaults() throws IOException {
		String dml = readResource("/db/changelog/sql/0075-DML-consent_required_states-table-insert.sql");
		Matcher matcher = SEED_ROW_PATTERN.matcher(dml);

		int rowCount = 0;
		boolean foundDc = false;
		boolean foundPr = false;

		while (matcher.find()) {
			rowCount++;
			String stateCode = matcher.group(1);
			String consentRequired = matcher.group(3);

			Assertions.assertThat(consentRequired).isEqualTo("false");

			if ("DC".equals(stateCode)) {
				foundDc = true;
			}

			if ("PR".equals(stateCode)) {
				foundPr = true;
			}
		}

		Assertions.assertThat(rowCount).isEqualTo(50);
		Assertions.assertThat(foundDc).isFalse();
		Assertions.assertThat(foundPr).isFalse();
	}

	private String readResource(String resourcePath) throws IOException {
		try (InputStream inputStream = getClass().getResourceAsStream(resourcePath)) {
			Assertions.assertThat(inputStream).isNotNull();
			return new String(inputStream.readAllBytes(), StandardCharsets.UTF_8);
		}
	}

}
