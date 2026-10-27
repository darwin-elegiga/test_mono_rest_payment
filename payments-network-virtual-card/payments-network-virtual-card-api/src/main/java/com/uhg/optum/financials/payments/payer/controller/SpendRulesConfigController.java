package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.SpendRulesDTO;
import com.uhg.optum.financials.payments.payer.dto.SpendRulesResponseDTO;
import com.uhg.optum.financials.payments.payer.model.SpendRules;
import com.uhg.optum.financials.payments.payer.service.SpendRulesConfigService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;
import java.util.Optional;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.INFO;

@RestController
@Slf4j
@Tag(name = "Spend Rules Association",
		description = "APIs for configuring and managing spend rules associated with virtual cards.")
@RequestMapping("/v1/spend-rules")
public class SpendRulesConfigController {

	@Autowired
	private LogUtility logUtility;

	@Autowired
	private SpendRulesConfigService spendRulesConfigService;

	@Deprecated
	@Operation(summary = "Spend Rules Configuration",
			description = " **NOTE**: This endpoint is used to configure list of spend rules for virtual cards. It allows users to set up rules that govern how virtual cards can be used, including limits on spending, categories of allowed transactions, and other parameters.")
	@PreAuthorize("hasAuthority('org:networks:write')")
	@PostMapping("/configure")
	public ResponseEntity<List<SpendRulesResponseDTO>> spendRulesConfig(@Valid @RequestBody SpendRulesDTO spendRulesDTO,
			@RequestHeader("tenantId") @Parameter(description = "e.g., \"ORG12345\"") String tenantId) {
		this.logUtility.logMessage("spend rule config request received : " + spendRulesDTO, INFO, log);
		this.spendRulesConfigService.loadSpendRuleConfigs(spendRulesDTO, tenantId);
		return ResponseEntity.ok().build();
	}

	@Operation(summary = "Spend Rules Configuration",
			description = " **NOTE**: This endpoint is used to configure list of spend rules for virtual cards. It allows users to set up rules that govern how virtual cards can be used, including limits on spending, categories of allowed transactions, and other parameters.")
	@PreAuthorize("hasAuthority('org:networks:write')")
	@PostMapping("/list/configure")
	public ResponseEntity<List<SpendRulesResponseDTO>> spendRulesConfigList(
			@Valid @RequestBody SpendRulesDTO spendRulesDTO,
			@RequestHeader("tenantId") @Parameter(description = "e.g., \"ORG12345\"") String tenantId) {
		this.logUtility.logMessage("spend rule config request received : " + spendRulesDTO, INFO, log);
		List<SpendRulesResponseDTO> response = this.spendRulesConfigService.loadSpendRuleConfigs(spendRulesDTO,
				tenantId);
		return new ResponseEntity<>(response, HttpStatus.OK);
	}

	@Operation(summary = "Update Spend Rules Configuration",
			description = "Endpoint to update spend rules configuration for virtual cards.")
	@PreAuthorize("hasAuthority('org:networks:write')")
	@PutMapping("/update")
	public ResponseEntity<List<SpendRulesResponseDTO>> updateSpendRulesConfig(
			@Valid @RequestBody SpendRulesDTO spendRulesDTO,
			@RequestHeader("tenantId") @Parameter(description = "e.g., \"ORG12345\"") String tenantId) {
		this.logUtility.logMessage("spend rule update config request received : " + spendRulesDTO, INFO, log);
		List<SpendRulesResponseDTO> response = this.spendRulesConfigService.updateSpendRuleConfigs(spendRulesDTO,
				tenantId);
		return new ResponseEntity<>(response, HttpStatus.OK);
	}

	@Operation(summary = "Get Spend Rules Configuration",
			description = "Endpoint to retrieve the current spend rules configuration for virtual cards.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/view/{orgShortCode}/{accountShortCode}/{subAccountShortCode}")
	public ResponseEntity<SpendRulesResponseDTO> getSpendRulesConfig(@PathVariable String orgShortCode,
			Optional<String> accountShortCode, Optional<String> subAccountShortCode) {
		SpendRulesResponseDTO response = this.spendRulesConfigService.getSpendRuleConfigs(orgShortCode,
				accountShortCode.orElse(null), subAccountShortCode.orElse(null));
		return new ResponseEntity<>(response, HttpStatus.OK);
	}

	@Operation(summary = "Get all Spend Rules Configurations",
			description = "Endpoint to retrieve all spend rules configurations for virtual cards.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/view/get-all-spend-rules")
	public ResponseEntity<List<SpendRules>> getAllSpendRules() {
		List<SpendRules> response = this.spendRulesConfigService.getAllSpendRuleConfigs();
		return new ResponseEntity<>(response, HttpStatus.OK);
	}

	// delete spend rules
	@Operation(summary = "Delete Spend Rules Configuration",
			description = "Endpoint to delete a specific spend rules configuration for virtual cards.")
	@PreAuthorize("hasAuthority('org:networks:write')")
	@DeleteMapping("/delete/{orgShortCode}")
	public ResponseEntity<SpendRulesResponseDTO> deleteSpendRulesConfig(@PathVariable String orgShortCode,
			@RequestParam(required = false) Optional<String> accountShortCode,
			@RequestParam(required = false) Optional<String> subAccountShortCode) {
		SpendRulesResponseDTO response = this.spendRulesConfigService.deleteSpendRuleConfigs(orgShortCode,
				accountShortCode.orElse(null), subAccountShortCode.orElse(null));
		return new ResponseEntity<>(response, HttpStatus.OK);
	}

}
