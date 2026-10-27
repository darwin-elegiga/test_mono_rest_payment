package com.uhg.optum.financials.payments.payer.model;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.assertj.core.api.Assertions;
import java.time.LocalDateTime;
import java.util.Arrays;

@DisplayName("SpendRules Entity Tests")
class SpendRulesTests {

	@Test
	@DisplayName("should set and get all fields correctly")
	void shouldSetAndGetAllFields() {
		// Arrange
		SpendRules spendRules = new SpendRules();
		this.setupSpendRules(spendRules);
		LocalDateTime now = LocalDateTime.now();
		spendRules.setCreatedAt(now);
		spendRules.setCreatedBy("creator");
		spendRules.setUpdatedAt(now);
		spendRules.setUpdatedBy("updator");
		spendRules.setId(123L);

		// Assert
		Assertions.assertThat(spendRules.getId()).isEqualTo(123L);
		Assertions.assertThat(spendRules.getRuleCode()).isEqualTo(Arrays.asList("RULE1", "RULE2"));
		Assertions.assertThat(spendRules.getOrgShortCode()).isEqualTo("ORG1");
		Assertions.assertThat(spendRules.getAccountShortCode()).isEqualTo("ACC1");
		Assertions.assertThat(spendRules.getSubAccountShortCode()).isEqualTo("SUB1");
		Assertions.assertThat(spendRules.getOrgName()).isEqualTo("Org Name");
		Assertions.assertThat(spendRules.getAccountName()).isEqualTo("Account Name");
		Assertions.assertThat(spendRules.getSubAccountName()).isEqualTo("Sub Account Name");
		Assertions.assertThat(spendRules.getCreatedAt()).isEqualTo(now);
		Assertions.assertThat(spendRules.getCreatedBy()).isEqualTo("creator");
		Assertions.assertThat(spendRules.getUpdatedAt()).isEqualTo(now);
		Assertions.assertThat(spendRules.getUpdatedBy()).isEqualTo("updator");
	}

	@Test
	@DisplayName("should initialize createdAt in default constructor")
	void shouldInitializeCreatedAtInConstructor() {
		// Act
		SpendRules spendRules = new SpendRules();
		// Assert
		Assertions.assertThat(spendRules.getCreatedAt()).isNotNull();
	}

	private void setupSpendRules(SpendRules spendRules) {
		spendRules.setRuleCode(Arrays.asList("RULE1", "RULE2"));
		spendRules.setOrgShortCode("ORG1");
		spendRules.setAccountShortCode("ACC1");
		spendRules.setSubAccountShortCode("SUB1");
		spendRules.setOrgName("Org Name");
		spendRules.setAccountName("Account Name");
		spendRules.setSubAccountName("Sub Account Name");
	}

}
