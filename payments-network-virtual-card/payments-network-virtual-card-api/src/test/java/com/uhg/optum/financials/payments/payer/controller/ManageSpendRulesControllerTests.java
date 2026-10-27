package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.constant.RuleType;
import com.uhg.optum.financials.payments.payer.dto.RuleDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.ManageSpendRules;
import com.uhg.optum.financials.payments.payer.service.ManageSpendRulesService;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.MockitoAnnotations;
import org.springframework.http.ResponseEntity;

import java.util.Collections;

class ManageSpendRulesControllerTests {

	@Mock
	private ManageSpendRulesService manageSpendRulesService;

	@InjectMocks
	private ManageSpendRulesController controller;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	void testAddSpendRule() {
		RuleDto ruleDto = new RuleDto();
		String expectedId = "123";
		BDDMockito.given(this.manageSpendRulesService.addSpendRule(ruleDto)).willReturn(expectedId);
		ResponseEntity<String> response = this.controller.addSpendRule(ruleDto);
		Assertions.assertThat(response).isNotNull();
		BDDMockito.then(this.manageSpendRulesService).should().addSpendRule(ruleDto);
	}

	@Test
	void testUpdateSpendRule() {
		RuleDto ruleDto = new RuleDto();
		ManageSpendRules manageSpendRules = new ManageSpendRules();
		BDDMockito.given(this.manageSpendRulesService.updateSpendRule(ruleDto)).willReturn(manageSpendRules);
		ResponseEntity<ManageSpendRules> response = this.controller.updateSpendRule(ruleDto);
		Assertions.assertThat(response).isNotNull();
		BDDMockito.then(this.manageSpendRulesService).should().updateSpendRule(ruleDto);
	}

	@Test
	void testGetSpendRuleById() {
		String ruleId = "123";
		ManageSpendRules manageSpendRules = new ManageSpendRules();
		BDDMockito.given(this.manageSpendRulesService.getSpendRuleById(ruleId)).willReturn(manageSpendRules);
		ResponseEntity<?> response = this.controller.getSpendRuleById(ruleId);
		Assertions.assertThat(response).isNotNull();
		Assertions.assertThat(response.getStatusCode().value()).isEqualTo(200);
		Assertions.assertThat(response.getBody()).isSameAs(manageSpendRules);
		BDDMockito.then(this.manageSpendRulesService).should().getSpendRuleById(ruleId);
	}

	@Test
	void testGetSpendRuleByIdNotFound() {
		String ruleId = "missing-rule";
		BDDMockito.given(this.manageSpendRulesService.getSpendRuleById(ruleId))
			.willThrow(new NotFoundException("Spend rule with ID " + ruleId + " not found."));

		Assertions.assertThatThrownBy(() -> this.controller.getSpendRuleById(ruleId))
			.isInstanceOf(NotFoundException.class)
			.hasMessage("Spend rule with ID " + ruleId + " not found.");
		BDDMockito.then(this.manageSpendRulesService).should().getSpendRuleById(ruleId);
	}

	@Test
	void testGetSpendRuleByIdBadRequest() {
		String ruleId = "   ";
		BDDMockito.given(this.manageSpendRulesService.getSpendRuleById(ruleId))
			.willThrow(new BadRequestException("Spend rule ID must be provided and cannot be empty."));

		Assertions.assertThatThrownBy(() -> this.controller.getSpendRuleById(ruleId))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("Spend rule ID must be provided and cannot be empty.");
		BDDMockito.then(this.manageSpendRulesService).should().getSpendRuleById(ruleId);
	}

	@Test
	void testGetRuleById() throws Exception {
		String ruleId = "456";
		ResponseEntity<?> expectedResponse = ResponseEntity.ok(new ManageSpendRules());
		Mockito.doReturn(expectedResponse).when(this.manageSpendRulesService).getRuleBasedOnId(ruleId);
		ResponseEntity<?> response = this.controller.getRuleById(ruleId);
		Assertions.assertThat(response).isNotNull();
		BDDMockito.then(this.manageSpendRulesService).should().getRuleBasedOnId(ruleId);
	}

	@Test
	void testGetSpendRulesByRuleTypeWithType() {
		com.uhg.optum.financials.payments.payer.constant.RuleType ruleType = RuleType.GLOBAL;
		ResponseEntity<?> expectedResponse = ResponseEntity.ok(Collections.emptyList());
		Mockito.doReturn(expectedResponse).when(this.manageSpendRulesService).getSpendRulesByType(ruleType);
		ResponseEntity<?> response = this.controller.getSpendRulesByRuleType(ruleType);
		Assertions.assertThat(response).isNotNull();
		BDDMockito.then(this.manageSpendRulesService).should().getSpendRulesByType(ruleType);
	}

}
