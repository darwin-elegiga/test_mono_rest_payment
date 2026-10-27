package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.constant.RuleType;
import com.uhg.optum.financials.payments.payer.dto.RuleDto;
import com.uhg.optum.financials.payments.payer.model.ManageSpendRules;
import com.uhg.optum.financials.payments.payer.service.ManageSpendRulesService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import jakarta.validation.Valid;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@Slf4j
@RequestMapping("/v1/spend-rule/")
public class ManageSpendRulesController {

	@Autowired
	private LogUtility logUtility;

	@Autowired
	private ManageSpendRulesService manageSpendRulesService;

	@Operation(summary = "Add Spend Rule (Internal)",
			description = "Internal-only endpoint to add a new spend rule. Uses database for persistence. Intended for mocking/testing.",
			tags = { "Internal Spend Rules" })
	@PreAuthorize("hasAuthority('org:networks:write')")
	@PostMapping("/add")
	public ResponseEntity<String> addSpendRule(@Valid @RequestBody RuleDto ruleDto) {
		// Implementation for adding spend rule
		String spendRuleId = this.manageSpendRulesService.addSpendRule(ruleDto);
		return ResponseEntity.ok(spendRuleId);
	}

	@Operation(summary = "Update Spend Rule (Internal)",
			description = "Internal-only endpoint to update an existing spend rule. Uses database for persistence. Intended for mocking/testing.",
			tags = { "Internal Spend Rules" })
	@PreAuthorize("hasAuthority('org:networks:write')")
	@PutMapping("/update")
	public ResponseEntity<ManageSpendRules> updateSpendRule(@Valid @RequestBody RuleDto ruleDto) {
		ManageSpendRules manageSpendRules = this.manageSpendRulesService.updateSpendRule(ruleDto);
		return ResponseEntity.ok(manageSpendRules);
	}

	@Operation(summary = "Get Spend Rule by ID (Internal)",
			description = "Internal-only endpoint to retrieve a spend rule from the database by its ID.",
			tags = { "Internal Spend Rules" })
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/get/{ruleId}")
	public ResponseEntity<ManageSpendRules> getSpendRuleById(@PathVariable String ruleId) {
		ManageSpendRules manageSpendRules = this.manageSpendRulesService.getSpendRuleById(ruleId);
		return ResponseEntity.ok(manageSpendRules);
	}

	@Operation(summary = "Get Spend Rules by Type (Portal)",
			description = "Used by the portal team. Returns rules based on type from downstream via service. If `ruleType` is omitted, returns **all** rules.",
			tags = { "Portal Spend Rules" })
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/get-by-type")
	public ResponseEntity<?> getSpendRulesByRuleType(@RequestParam(required = false) RuleType ruleType) {
		return ResponseEntity.ok(this.manageSpendRulesService.getSpendRulesByType(ruleType));
	}

	@Operation(summary = "Get all Spend Rules (Downstream wrapper)",
			description = "Direct pass-through to downstream to list rule configurations. Supports pagination.",
			tags = { "Downstream Spend Rules" })
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/rules")
	public ResponseEntity<?> getAllRules(@RequestParam(required = false, defaultValue = "1") Integer page,
			@RequestParam(required = false, defaultValue = "5") Integer size,
			@RequestParam(required = false) RuleType ruleType) throws Exception {
		return this.manageSpendRulesService.getRulesBasedOnTypeAndPagination(page, size, ruleType);
	}

	@Operation(summary = "Get Rule by ID (Downstream wrapper)",
			description = "Direct pass-through to downstream to fetch a single rule using its ID.",
			tags = { "Downstream Spend Rules" })
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/rules/{ruleId}")
	public ResponseEntity<?> getRuleById(@PathVariable String ruleId) throws Exception {
		return this.manageSpendRulesService.getRuleBasedOnId(ruleId);
	}

}
