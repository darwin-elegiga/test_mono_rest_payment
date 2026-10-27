package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.SpendRulesDTO;
import com.uhg.optum.financials.payments.payer.dto.SpendRulesResponseDTO;
import com.uhg.optum.financials.payments.payer.model.SpendRules;
import com.uhg.optum.financials.payments.payer.service.SpendRulesConfigService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import java.util.Arrays;
import java.util.List;
import java.util.Optional;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.BDDMockito.then;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.willDoNothing;

@ExtendWith(MockitoExtension.class)
public class SpendRulesConfigControllerTests {

	@InjectMocks
	private SpendRulesConfigController spendRulesConfigController;

	@Mock
	private SpendRulesConfigService spendRulesConfigService;

	@Mock
	private LogUtility logUtility;

	@Test
	public void testSpendRulesConfigList() {
		SpendRulesDTO spendRulesDTO = new SpendRulesDTO();
		String tenantId = "ORG12345";
		SpendRulesResponseDTO responseDTO1 = new SpendRulesResponseDTO();
		SpendRulesResponseDTO responseDTO2 = new SpendRulesResponseDTO();
		List<SpendRulesResponseDTO> mockResponse = Arrays.asList(responseDTO1, responseDTO2);

		BDDMockito.when(this.spendRulesConfigService.loadSpendRuleConfigs(spendRulesDTO, tenantId))
			.thenReturn(mockResponse);

		ResponseEntity<List<SpendRulesResponseDTO>> response = this.spendRulesConfigController
			.spendRulesConfigList(spendRulesDTO, tenantId);

		Assertions.assertThat(response.getStatusCode().value()).isEqualTo(200);
		Assertions.assertThat(response.getBody()).isEqualTo(mockResponse);
	}

	@Test
	public void testGetAllSpendRules() {
		SpendRules rule1 = new SpendRules();
		SpendRules rule2 = new SpendRules();
		List<SpendRules> mockList = Arrays.asList(rule1, rule2);

		BDDMockito.when(this.spendRulesConfigService.getAllSpendRuleConfigs()).thenReturn(mockList);

		ResponseEntity<List<SpendRules>> response = this.spendRulesConfigController.getAllSpendRules();
		Assertions.assertThat(response.getStatusCode().value()).isEqualTo(200);
		Assertions.assertThat(response.getBody()).isEqualTo(mockList);
	}

	@Test
	public void testDeleteSpendRulesConfig() {
		String orgShortCode = "ORG123";
		Optional<String> accountShortCode = Optional.of("ACC456");
		Optional<String> subAccountShortCode = Optional.of("SUB789");
		SpendRulesResponseDTO mockResponse = new SpendRulesResponseDTO();

		BDDMockito
			.when(this.spendRulesConfigService.deleteSpendRuleConfigs(orgShortCode, accountShortCode.orElse(null),
					subAccountShortCode.orElse(null)))
			.thenReturn(mockResponse);

		ResponseEntity<SpendRulesResponseDTO> response = this.spendRulesConfigController
			.deleteSpendRulesConfig(orgShortCode, accountShortCode, subAccountShortCode);

		Assertions.assertThat(response.getStatusCode().value()).isEqualTo(200);
		Assertions.assertThat(response.getBody()).isEqualTo(mockResponse);
	}

	@Test
	void spendRulesConfig_returnsOkAndLogsRequest() {
		SpendRulesDTO spendRulesDTO = new SpendRulesDTO();
		String tenantId = "ORG12345";
		List<SpendRulesResponseDTO> responseList = List.of(new SpendRulesResponseDTO());
		given(this.spendRulesConfigService.loadSpendRuleConfigs(eq(spendRulesDTO), eq(tenantId)))
			.willReturn(responseList);
		willDoNothing().given(this.logUtility)
			.logMessage(any(String.class), any(String.class), any(org.slf4j.Logger.class));

		ResponseEntity<List<SpendRulesResponseDTO>> response = this.spendRulesConfigController
			.spendRulesConfig(spendRulesDTO, tenantId);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isNull();
		then(this.logUtility).should().logMessage(any(String.class), any(String.class), any(org.slf4j.Logger.class));
	}

	@Test
	void spendRulesConfigList_returnsResponseListAndLogsRequest() {
		SpendRulesDTO spendRulesDTO = new SpendRulesDTO();
		String tenantId = "ORG12345";
		List<SpendRulesResponseDTO> responseList = List.of(new SpendRulesResponseDTO());
		given(this.spendRulesConfigService.loadSpendRuleConfigs(eq(spendRulesDTO), eq(tenantId)))
			.willReturn(responseList);
		willDoNothing().given(this.logUtility)
			.logMessage(any(String.class), any(String.class), any(org.slf4j.Logger.class));

		ResponseEntity<List<SpendRulesResponseDTO>> response = this.spendRulesConfigController
			.spendRulesConfigList(spendRulesDTO, tenantId);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(responseList);
		then(this.logUtility).should().logMessage(any(String.class), any(String.class), any(org.slf4j.Logger.class));
	}

	@Test
	void updateSpendRulesConfig_returnsUpdatedListAndLogsRequest() {
		SpendRulesDTO spendRulesDTO = new SpendRulesDTO();
		String tenantId = "ORG12345";
		List<SpendRulesResponseDTO> updatedList = List.of(new SpendRulesResponseDTO());
		given(this.spendRulesConfigService.updateSpendRuleConfigs(eq(spendRulesDTO), eq(tenantId)))
			.willReturn(updatedList);
		willDoNothing().given(this.logUtility)
			.logMessage(any(String.class), any(String.class), any(org.slf4j.Logger.class));

		ResponseEntity<List<SpendRulesResponseDTO>> response = this.spendRulesConfigController
			.updateSpendRulesConfig(spendRulesDTO, tenantId);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(updatedList);
		then(this.logUtility).should().logMessage(any(String.class), any(String.class), any(org.slf4j.Logger.class));
	}

	@Test
	void getSpendRulesConfig_returnsConfig() {
		String orgShortCode = "ORG12345";
		Optional<String> accountShortCode = Optional.of("ACC123");
		Optional<String> subAccountShortCode = Optional.of("SUB123");
		SpendRulesResponseDTO responseDTO = new SpendRulesResponseDTO();
		given(this.spendRulesConfigService.getSpendRuleConfigs(eq(orgShortCode), eq(accountShortCode.get()),
				eq(subAccountShortCode.get())))
			.willReturn(responseDTO);

		ResponseEntity<SpendRulesResponseDTO> response = this.spendRulesConfigController
			.getSpendRulesConfig(orgShortCode, accountShortCode, subAccountShortCode);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(responseDTO);
	}

	@Test
	void getSpendRulesConfig_returnsConfigWithNullOptionals() {
		String orgShortCode = "ORG12345";
		Optional<String> accountShortCode = Optional.empty();
		Optional<String> subAccountShortCode = Optional.empty();
		SpendRulesResponseDTO responseDTO = new SpendRulesResponseDTO();
		given(this.spendRulesConfigService.getSpendRuleConfigs(eq(orgShortCode), eq((String) null), eq((String) null)))
			.willReturn(responseDTO);

		ResponseEntity<SpendRulesResponseDTO> response = this.spendRulesConfigController
			.getSpendRulesConfig(orgShortCode, accountShortCode, subAccountShortCode);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(responseDTO);
	}

	@Test
	void getAllSpendRules_returnsAllConfigs() {
		List<SpendRules> spendRulesList = List.of(new SpendRules());
		given(this.spendRulesConfigService.getAllSpendRuleConfigs()).willReturn(spendRulesList);

		ResponseEntity<List<SpendRules>> response = this.spendRulesConfigController.getAllSpendRules();
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(spendRulesList);
	}

	@Test
	void deleteSpendRulesConfig_returnsDeletedConfig() {
		String orgShortCode = "ORG12345";
		Optional<String> accountShortCode = Optional.of("ACC123");
		Optional<String> subAccountShortCode = Optional.of("SUB123");
		SpendRulesResponseDTO responseDTO = new SpendRulesResponseDTO();
		given(this.spendRulesConfigService.deleteSpendRuleConfigs(eq(orgShortCode), eq(accountShortCode.get()),
				eq(subAccountShortCode.get())))
			.willReturn(responseDTO);

		ResponseEntity<SpendRulesResponseDTO> response = this.spendRulesConfigController
			.deleteSpendRulesConfig(orgShortCode, accountShortCode, subAccountShortCode);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(responseDTO);
	}

	@Test
	void deleteSpendRulesConfig_returnsDeletedConfigWithNullOptionals() {
		String orgShortCode = "ORG12345";
		Optional<String> accountShortCode = Optional.empty();
		Optional<String> subAccountShortCode = Optional.empty();
		SpendRulesResponseDTO responseDTO = new SpendRulesResponseDTO();
		given(this.spendRulesConfigService.deleteSpendRuleConfigs(eq(orgShortCode), eq((String) null),
				eq((String) null)))
			.willReturn(responseDTO);

		ResponseEntity<SpendRulesResponseDTO> response = this.spendRulesConfigController
			.deleteSpendRulesConfig(orgShortCode, accountShortCode, subAccountShortCode);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(responseDTO);
	}

}
