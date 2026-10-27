package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.RuleType;
import com.uhg.optum.financials.payments.payer.dto.RuleDto;
import com.uhg.optum.financials.payments.payer.dto.RuleResponse;
import com.uhg.optum.financials.payments.payer.dto.RulesResponse;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.ManageSpendRules;
import com.uhg.optum.financials.payments.payer.repository.ManageSpendRulesRepository;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.ModelConversionMapper;
import org.assertj.core.api.BDDAssertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import java.time.LocalDateTime;
import static org.mockito.ArgumentMatchers.anyInt;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.times;

@ExtendWith(MockitoExtension.class)
class ManageSpendRulesServiceImplTests {

	@Mock
	private ManageSpendRulesRepository manageSpendRulesRepository;

	@Mock
	private ModelConversionMapper modelConversionMapper;

	@Mock
	private ZetaApiService zetaApiService;

	@Mock
	private LogUtility logUtility;

	@InjectMocks
	private ManageSpendRulesServiceImpl service;

	@Test
	void addSpendRule_shouldSaveAndReturnRuleId_whenRuleDoesNotExist() {
		RuleDto ruleDto = new RuleDto();
		ruleDto.setRuleId("R1");

		ManageSpendRules mapped = new ManageSpendRules();
		ManageSpendRules saved = new ManageSpendRules();
		saved.setRuleId("R1");

		BDDMockito.given(this.manageSpendRulesRepository.findByRuleId("R1")).willReturn(null);
		BDDMockito.given(this.modelConversionMapper.spendRuleModelMapper(ruleDto, ManageSpendRules.class))
			.willReturn(mapped);
		BDDMockito.given(this.manageSpendRulesRepository.save(mapped)).willReturn(saved);

		String result = this.service.addSpendRule(ruleDto);

		BDDAssertions.then(result).isEqualTo("R1");
		BDDMockito.then(this.manageSpendRulesRepository).should(times(1)).save(mapped);
	}

	@Test
	void addSpendRule_shouldThrowBadRequest_whenRuleAlreadyExists() {
		RuleDto ruleDto = new RuleDto();
		ruleDto.setRuleId("DUPLICATE_ID");

		BDDMockito.given(this.manageSpendRulesRepository.findByRuleId("DUPLICATE_ID"))
			.willReturn(new ManageSpendRules());

		BDDAssertions.thenThrownBy(() -> this.service.addSpendRule(ruleDto))
			.isInstanceOf(BadRequestException.class)
			.hasMessageContaining("already exists");
	}

	@Test
	void updateSpendRule_shouldSave_whenRuleExists() {
		RuleDto ruleDto = new RuleDto();
		ruleDto.setRuleId("R1");

		ManageSpendRules existing = new ManageSpendRules();
		existing.setId(10L);
		existing.setCreatedBy("creator");
		existing.setCreatedAt(LocalDateTime.now().minusDays(2));

		ManageSpendRules mapped = new ManageSpendRules();
		ManageSpendRules saved = new ManageSpendRules();
		saved.setRuleId("R1");

		BDDMockito.given(this.manageSpendRulesRepository.findByRuleId("R1")).willReturn(existing);
		BDDMockito.given(this.modelConversionMapper.spendRuleModelMapper(ruleDto, ManageSpendRules.class))
			.willReturn(mapped);
		BDDMockito.given(this.manageSpendRulesRepository.save(mapped)).willReturn(saved);

		ManageSpendRules result = this.service.updateSpendRule(ruleDto);

		BDDAssertions.then(result).isSameAs(saved);
		BDDAssertions.then(mapped.getId()).isEqualTo(10L);
		BDDAssertions.then(mapped.getCreatedBy()).isEqualTo("creator");
		BDDAssertions.then(mapped.getCreatedAt()).isEqualTo(existing.getCreatedAt());
		BDDAssertions.then(mapped.getUpdatedBy()).isNotBlank();
		BDDAssertions.then(mapped.getUpdatedAt()).isNotNull();
	}

	@Test
	void updateSpendRule_shouldThrowBadRequest_whenRuleMissing() {
		RuleDto ruleDto = new RuleDto();
		ruleDto.setRuleId("MISSING");

		BDDMockito.given(this.manageSpendRulesRepository.findByRuleId("MISSING")).willReturn(null);

		BDDAssertions.thenThrownBy(() -> this.service.updateSpendRule(ruleDto))
			.isInstanceOf(BadRequestException.class)
			.hasMessageContaining("does not exist");
	}

	@Test
	void getSpendRuleById_shouldReturnEntity() {
		ManageSpendRules expected = new ManageSpendRules();
		expected.setRuleId("R3");

		BDDMockito.given(this.manageSpendRulesRepository.findByRuleId("R3")).willReturn(expected);

		ManageSpendRules result = this.service.getSpendRuleById("R3");

		BDDAssertions.then(result).isSameAs(expected);
	}

	@Test
	void getSpendRuleById_shouldThrowBadRequest_whenRuleIdBlank() {
		BDDAssertions.thenThrownBy(() -> this.service.getSpendRuleById("   "))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("Spend rule ID must be provided and cannot be empty.");
	}

	@Test
	void getSpendRuleById_shouldThrowNotFound_whenRuleMissing() {
		String ruleId = "missing-rule";
		BDDMockito.given(this.manageSpendRulesRepository.findByRuleId(ruleId)).willReturn(null);

		BDDAssertions.thenThrownBy(() -> this.service.getSpendRuleById(ruleId))
			.isInstanceOf(NotFoundException.class)
			.hasMessage("Spend rule with ID " + ruleId + " not found.");
	}

	@Test
	void getSpendRulesByType_shouldReturnOk_whenDownstreamSuccess() throws Exception {
		String body = "{\"data\":[{\"ruleType\":\"Custom\",\"createdAt\":\"1700000000000\",\"updatedAt\":\"1700000000000\"}]}";
		BDDMockito.given(this.zetaApiService.getAllRules(1, Integer.MAX_VALUE, "Custom"))
			.willReturn(new ResponseEntity<>(body, HttpStatus.OK));

		ResponseEntity<?> result = this.service.getSpendRulesByType(RuleType.CUSTOM);

		BDDAssertions.then(result).isNotNull();
		BDDAssertions.then(result.getStatusCode()).isEqualTo(HttpStatus.OK);
		BDDAssertions.then(result.getBody()).isInstanceOf(RulesResponse.class);

		RulesResponse rulesResponse = (RulesResponse) result.getBody();
		BDDAssertions.then(rulesResponse.getData()).hasSize(1);
		BDDAssertions.then(rulesResponse.getData().get(0).getRuleType()).isEqualTo("CUSTOM");
		BDDAssertions.then(rulesResponse.getData().get(0).getCreatedAt()).isNotBlank();
		BDDAssertions.then(rulesResponse.getData().get(0).getUpdatedAt()).isNotBlank();
	}

	@Test
	void getSpendRulesByType_shouldReturnNull_whenExceptionOccurs() throws Exception {
		BDDMockito.given(this.zetaApiService.getAllRules(anyInt(), anyInt(), anyString()))
			.willThrow(new RuntimeException("downstream"));

		BDDAssertions.thenThrownBy(() -> this.service.getSpendRulesByType(RuleType.CUSTOM))
			.isInstanceOf(RuntimeException.class)
			.hasMessage("downstream");
	}

	@Test
	void getAllRules_shouldReturnBadRequest_whenPageNull() throws Exception {
		BDDAssertions.thenThrownBy(() -> this.service.getAllRules(null, 10, "Custom"))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("Invalid page number. Page number must be a positive integer (at least 1).");
	}

	@Test
	void getAllRules_shouldReturnBadRequest_whenPageLessThanOne() throws Exception {
		BDDAssertions.thenThrownBy(() -> this.service.getAllRules(0, 10, "Custom"))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("Invalid page number. Page number must be a positive integer (at least 1).");
	}

	@Test
	void getAllRules_shouldReturnBadRequest_whenSizeNull() throws Exception {
		BDDAssertions.thenThrownBy(() -> this.service.getAllRules(1, null, "Custom"))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("Invalid size number. Size number must be a positive integer.");
	}

	@Test
	void getAllRules_shouldReturnBadRequest_whenSizeLessThanOne() throws Exception {
		BDDAssertions.thenThrownBy(() -> this.service.getAllRules(1, 0, "Custom"))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("Invalid size number. Size number must be a positive integer.");
	}

	@Test
	void getAllRules_shouldThrowRuntimeException_whenBodyNull() throws Exception {
		BDDMockito.given(this.zetaApiService.getAllRules(1, 10, "Custom")).willReturn(ResponseEntity.ok().build());

		BDDAssertions.thenThrownBy(() -> this.service.getAllRules(1, 10, "Custom"))
			.isInstanceOf(RuntimeException.class)
			.hasMessageContaining("Empty response body received from get all rules API");
	}

	@Test
	void getAllRules_shouldThrowRuntimeException_whenBodyEmpty() throws Exception {
		BDDMockito.given(this.zetaApiService.getAllRules(1, 10, "Custom"))
			.willReturn(new ResponseEntity<>("", HttpStatus.OK));

		BDDAssertions.thenThrownBy(() -> this.service.getAllRules(1, 10, "Custom"))
			.isInstanceOf(RuntimeException.class)
			.hasMessageContaining("Empty response body received from get all rules API");
	}

	@Test
	void getAllRules_shouldReturnErrorResponse_whenNon2xx() throws Exception {
		String errorBody = "{\"errorCode\":\"ERR\",\"errorMessage\":\"failed\"}";
		BDDMockito.given(this.zetaApiService.getAllRules(1, 10, "Custom"))
			.willReturn(new ResponseEntity<>(errorBody, HttpStatus.BAD_REQUEST));

		BDDAssertions.thenThrownBy(() -> this.service.getAllRules(1, 10, "Custom"))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("failed");
	}

	@Test
	void getAllRules_shouldMapRequiredToGlobal_whenSuccessful() throws Exception {
		String body = "{\"data\":[{\"ruleType\":\"Required\",\"createdAt\":\"1700000000000\",\"updatedAt\":\"1700000000000\"}]}";
		BDDMockito.given(this.zetaApiService.getAllRules(1, 10, "Required"))
			.willReturn(new ResponseEntity<>(body, HttpStatus.OK));

		ResponseEntity<?> result = this.service.getAllRules(1, 10, "Required");

		BDDAssertions.then(result.getStatusCode()).isEqualTo(HttpStatus.OK);
		RulesResponse rulesResponse = (RulesResponse) result.getBody();
		BDDAssertions.then(rulesResponse.getData().get(0).getRuleType()).isEqualTo(RuleType.GLOBAL.name());
	}

	@Test
	void getRuleBasedOnId_shouldReturnBadRequest_whenRuleIdNull() throws Exception {
		BDDAssertions.thenThrownBy(() -> this.service.getRuleBasedOnId(null))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("Rule ID must be provided and cannot be empty.");
	}

	@Test
	void getRuleBasedOnId_shouldReturnBadRequest_whenRuleIdEmpty() throws Exception {
		BDDAssertions.thenThrownBy(() -> this.service.getRuleBasedOnId(""))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("Rule ID must be provided and cannot be empty.");
	}

	@Test
	void getRuleBasedOnId_shouldThrowRuntimeException_whenBodyNull() throws Exception {
		BDDMockito.given(this.zetaApiService.getRuleById("R1")).willReturn(ResponseEntity.ok().build());

		BDDAssertions.thenThrownBy(() -> this.service.getRuleBasedOnId("R1"))
			.isInstanceOf(RuntimeException.class)
			.hasMessageContaining("Empty response body received from get rule by id API");
	}

	@Test
	void getRuleBasedOnId_shouldThrowRuntimeException_whenBodyEmpty() throws Exception {
		BDDMockito.given(this.zetaApiService.getRuleById("R1")).willReturn(new ResponseEntity<>("", HttpStatus.OK));

		BDDAssertions.thenThrownBy(() -> this.service.getRuleBasedOnId("R1"))
			.isInstanceOf(RuntimeException.class)
			.hasMessageContaining("Empty response body received from get rule by id API");
	}

	@Test
	void getRuleBasedOnId_shouldReturnErrorResponse_whenNon2xx() throws Exception {
		BDDMockito.given(this.zetaApiService.getRuleById("R1"))
			.willReturn(new ResponseEntity<>("{\"errorCode\":\"NOT_FOUND\",\"errorMessage\":\"Rule not found\"}",
					HttpStatus.NOT_FOUND));

		BDDAssertions.thenThrownBy(() -> this.service.getRuleBasedOnId("R1"))
			.isInstanceOf(NotFoundException.class)
			.hasMessage("Rule not found");
	}

	@Test
	void getRuleBasedOnId_shouldMapRequiredToGlobal_whenSuccessful() throws Exception {
		String body = "{\"ruleType\":\"Required\",\"createdAt\":\"1700000000000\",\"updatedAt\":\"1700000000000\"}";
		BDDMockito.given(this.zetaApiService.getRuleById("R1")).willReturn(new ResponseEntity<>(body, HttpStatus.OK));

		ResponseEntity<?> result = this.service.getRuleBasedOnId("R1");

		BDDAssertions.then(result.getStatusCode()).isEqualTo(HttpStatus.OK);
		RuleResponse ruleResponse = (RuleResponse) result.getBody();
		BDDAssertions.then(ruleResponse.getRuleType()).isEqualTo(RuleType.GLOBAL.name());
		BDDAssertions.then(ruleResponse.getCreatedAt()).isNotBlank();
		BDDAssertions.then(ruleResponse.getUpdatedAt()).isNotBlank();
	}

	@Test
	void getRuleBasedOnId_shouldUpperCaseRuleType_whenNonRequiredSuccessful() throws Exception {
		String body = "{\"ruleType\":\"custom\",\"createdAt\":\"1700000000000\",\"updatedAt\":\"1700000000000\"}";
		BDDMockito.given(this.zetaApiService.getRuleById("R2")).willReturn(new ResponseEntity<>(body, HttpStatus.OK));

		ResponseEntity<?> result = this.service.getRuleBasedOnId("R2");

		RuleResponse ruleResponse = (RuleResponse) result.getBody();
		BDDAssertions.then(ruleResponse.getRuleType()).isEqualTo("CUSTOM");
	}

	@Test
	void getRulesBasedOnTypeAndPagination_shouldPassRequiredForGlobal() throws Exception {
		String body = "{\"data\":[{\"ruleType\":\"Required\",\"createdAt\":\"1700000000000\",\"updatedAt\":\"1700000000000\"}]}";
		BDDMockito.given(this.zetaApiService.getAllRules(1, 20, "Required"))
			.willReturn(new ResponseEntity<>(body, HttpStatus.OK));

		ResponseEntity<?> result = this.service.getRulesBasedOnTypeAndPagination(1, 20, RuleType.GLOBAL);

		BDDAssertions.then(result.getStatusCode()).isEqualTo(HttpStatus.OK);
	}

	@Test
	void getRulesBasedOnTypeAndPagination_shouldPassCustomForCustom() throws Exception {
		String body = "{\"data\":[{\"ruleType\":\"Custom\",\"createdAt\":\"1700000000000\",\"updatedAt\":\"1700000000000\"}]}";
		BDDMockito.given(this.zetaApiService.getAllRules(1, 20, "Custom"))
			.willReturn(new ResponseEntity<>(body, HttpStatus.OK));

		ResponseEntity<?> result = this.service.getRulesBasedOnTypeAndPagination(1, 20, RuleType.CUSTOM);

		BDDAssertions.then(result.getStatusCode()).isEqualTo(HttpStatus.OK);
	}

	@Test
	void getRulesBasedOnTypeAndPagination_shouldPassGeneralForGeneral() throws Exception {
		String body = "{\"data\":[{\"ruleType\":\"General\",\"createdAt\":\"1700000000000\",\"updatedAt\":\"1700000000000\"}]}";
		BDDMockito.given(this.zetaApiService.getAllRules(1, 20, "General"))
			.willReturn(new ResponseEntity<>(body, HttpStatus.OK));

		ResponseEntity<?> result = this.service.getRulesBasedOnTypeAndPagination(1, 20, RuleType.GENERAL);

		BDDAssertions.then(result.getStatusCode()).isEqualTo(HttpStatus.OK);
	}

	@Test
	void getRulesBasedOnTypeAndPagination_shouldPassNull_whenRuleTypeNull() throws Exception {
		String body = "{\"data\":[{\"ruleType\":\"Custom\",\"createdAt\":\"1700000000000\",\"updatedAt\":\"1700000000000\"}]}";
		BDDMockito.given(this.zetaApiService.getAllRules(1, 20, null))
			.willReturn(new ResponseEntity<>(body, HttpStatus.OK));

		ResponseEntity<?> result = this.service.getRulesBasedOnTypeAndPagination(1, 20, null);

		BDDAssertions.then(result.getStatusCode()).isEqualTo(HttpStatus.OK);
	}

	@Test
	void getAllRules_shouldHandleInvalidMillisByReturningEmptyDateStrings() throws Exception {
		String body = "{\"data\":[{\"ruleType\":\"Custom\",\"createdAt\":\"invalid\",\"updatedAt\":\"invalid\"}]}";
		BDDMockito.given(this.zetaApiService.getAllRules(1, 10, "Custom"))
			.willReturn(new ResponseEntity<>(body, HttpStatus.OK));

		ResponseEntity<?> result = this.service.getAllRules(1, 10, "Custom");

		RulesResponse rulesResponse = (RulesResponse) result.getBody();
		BDDAssertions.then(rulesResponse.getData().get(0).getCreatedAt()).isEqualTo("");
		BDDAssertions.then(rulesResponse.getData().get(0).getUpdatedAt()).isEqualTo("");
	}

}
