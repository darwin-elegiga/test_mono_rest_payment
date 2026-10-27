package com.uhg.optum.financials.payments.payer.service.impl;

import com.google.gson.Gson;
import com.uhg.optum.financials.payments.payer.constant.RuleType;
import com.uhg.optum.financials.payments.payer.dto.ErrorResponse;
import com.uhg.optum.financials.payments.payer.dto.RuleDto;
import com.uhg.optum.financials.payments.payer.dto.RuleResponse;
import com.uhg.optum.financials.payments.payer.dto.RulesResponse;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.util.HttpExceptionUtil;
import com.uhg.optum.financials.payments.payer.model.ManageSpendRules;
import com.uhg.optum.financials.payments.payer.repository.ManageSpendRulesRepository;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.ModelConversionMapper;
import com.uhg.optum.financials.payments.payer.util.UserNameExtractUtil;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.time.LocalDateTime;
import java.time.ZoneId;
import java.util.List;
import java.util.stream.Collectors;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.REQUIRED;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;

@Slf4j
@Service
public class ManageSpendRulesServiceImpl
		implements com.uhg.optum.financials.payments.payer.service.ManageSpendRulesService {

	private final ManageSpendRulesRepository manageSpendRulesRepository;

	private final ModelConversionMapper modelConversionMapper;

	private final LogUtility logUtility;

	private final ZetaApiService zetaApiService;

	private static final Gson JSON = new Gson();

	public ManageSpendRulesServiceImpl(ManageSpendRulesRepository manageSpendRulesRepository,
			ModelConversionMapper modelConversionMapper, LogUtility logUtility, ZetaApiService zetaApiService) {
		this.manageSpendRulesRepository = manageSpendRulesRepository;
		this.modelConversionMapper = modelConversionMapper;
		this.logUtility = logUtility;
		this.zetaApiService = zetaApiService;
	}

	@Override
	public String addSpendRule(RuleDto ruleDto) {
		if (this.manageSpendRulesRepository.findByRuleId(ruleDto.getRuleId()) != null) {
			throw new BadRequestException("Spend rule with ID " + ruleDto.getRuleId() + " already exists.");
		}
		ManageSpendRules spendRule = this.modelConversionMapper.spendRuleModelMapper(ruleDto, ManageSpendRules.class);
		spendRule.setId(null); // Ensure ID is null for new record
		spendRule.setCreatedBy(UserNameExtractUtil.getUserNameFromToken());
		spendRule.setCreatedAt(LocalDateTime.now());
		ManageSpendRules savedRule = this.manageSpendRulesRepository.save(spendRule);
		return savedRule.getRuleId();
	}

	@Override
	public ManageSpendRules updateSpendRule(RuleDto ruleDto) {
		ManageSpendRules existingSpendRule = this.manageSpendRulesRepository.findByRuleId(ruleDto.getRuleId());
		if (existingSpendRule == null) {
			throw new BadRequestException("Spend rule with ID " + ruleDto.getRuleId() + " does not exist.");
		}
		ManageSpendRules spendRule = this.modelConversionMapper.spendRuleModelMapper(ruleDto, ManageSpendRules.class);
		spendRule.setId(existingSpendRule.getId()); // Preserve the existing ID
		spendRule.setCreatedAt(existingSpendRule.getCreatedAt());
		spendRule.setCreatedBy(existingSpendRule.getCreatedBy());
		spendRule.setUpdatedBy(UserNameExtractUtil.getUserNameFromToken());
		spendRule.setUpdatedAt(LocalDateTime.now());
		return this.manageSpendRulesRepository.save(spendRule);
	}

	@Override
	public ManageSpendRules getSpendRuleById(String ruleId) {
		if (ruleId == null || ruleId.isBlank()) {
			throw new BadRequestException("Spend rule ID must be provided and cannot be empty.");
		}

		ManageSpendRules manageSpendRules = this.manageSpendRulesRepository.findByRuleId(ruleId);
		if (manageSpendRules == null) {
			throw new NotFoundException("Spend rule with ID " + ruleId + " not found.");
		}

		return manageSpendRules;
	}

	@Override
	public ResponseEntity<?> getSpendRulesByType(RuleType ruleType) {

		String rule = getRuleTypeToPassToZeta(ruleType);
		try {
			return getAllRules(1, Integer.MAX_VALUE, rule);
		}
		catch (Exception ex) {
			this.logUtility.logMessage("Error occurred while fetching rules by type: " + ruleType, ERROR, log);
			throw (ex instanceof RuntimeException runtimeException) ? runtimeException
					: new RuntimeException(ex.getMessage(), ex);
		}
	}

	@Override
	public ResponseEntity<?> getAllRules(Integer page, Integer size, String ruleType) throws Exception {
		if (page == null || page < 1) {
			throw new BadRequestException("Invalid page number. Page number must be a positive integer (at least 1).");
		}

		if (size == null || size < 1) {
			throw new BadRequestException("Invalid size number. Size number must be a positive integer.");
		}

		this.logUtility.logMessage("Calling zeta api for rules", INFO, log);
		ResponseEntity<String> response = this.zetaApiService.getAllRules(page, size, ruleType);
		String body = response.getBody();
		if (body == null || body.isEmpty()) {
			this.logUtility.logMessage("Empty response body received from get all rules API", INFO, log);
			throw new RuntimeException("Empty response body received from get all rules API ");
		}

		if (!response.getStatusCode().is2xxSuccessful()) {
			this.logUtility.logMessage("Error occurred while fetching all rules ", ERROR, log);
			ErrorResponse errorResponse = JSON.fromJson(body, ErrorResponse.class);
			throw HttpExceptionUtil.buildException(response.getStatusCode(), getErrorMessage(errorResponse));
		}

		RulesResponse rulesResponse = JSON.fromJson(body, RulesResponse.class);

		this.logUtility.logMessage("Preparing final response ", INFO, log);
		List<RulesResponse.RuleViewResponse> ruleResponses = rulesResponse.getData().stream().peek((rule) -> {
			if (rule.getRuleType().equalsIgnoreCase(REQUIRED)) {
				rule.setRuleType(RuleType.GLOBAL.name());
			}
			else {
				rule.setRuleType(rule.getRuleType().toUpperCase());
			}
			rule.setCreatedAt(convertMilliSecToLocalDateTime(rule.getCreatedAt()));
			rule.setUpdatedAt(convertMilliSecToLocalDateTime(rule.getUpdatedAt()));
		}).collect(Collectors.toList());

		rulesResponse.setData(ruleResponses);
		this.logUtility.logMessage("Final response prepared successfully ", INFO, log);

		this.logUtility.logMessage("Rules fetched successfully", INFO, log);
		return new ResponseEntity<>(rulesResponse, HttpStatus.OK);

	}

	@Override
	public ResponseEntity<?> getRuleBasedOnId(String ruleId) throws Exception {
		if (ruleId == null || ruleId.isBlank()) {
			throw new BadRequestException("Rule ID must be provided and cannot be empty.");
		}
		ResponseEntity<String> response = this.zetaApiService.getRuleById(ruleId);
		String body = response.getBody();
		if (body == null || body.isEmpty()) {
			this.logUtility.logMessage("Empty response body received from getRuleById API", INFO, log);
			throw new RuntimeException("Empty response body received from get rule by id API");
		}

		if (!response.getStatusCode().is2xxSuccessful()) {
			this.logUtility.logMessage("Error occurred while fetching rule on basic of id ", ERROR, log);
			ErrorResponse errorResponse = JSON.fromJson(body, ErrorResponse.class);
			throw HttpExceptionUtil.buildException(response.getStatusCode(), getErrorMessage(errorResponse));
		}

		RuleResponse ruleResponse = JSON.fromJson(body, RuleResponse.class);
		String ruleType = ruleResponse.getRuleType();
		if (ruleType.equalsIgnoreCase(REQUIRED)) {
			ruleResponse.setRuleType(RuleType.GLOBAL.name());
		}
		else {
			ruleResponse.setRuleType(ruleType.toUpperCase());
		}
		ruleResponse.setCreatedAt(convertMilliSecToLocalDateTime(ruleResponse.getCreatedAt()));
		ruleResponse.setUpdatedAt(convertMilliSecToLocalDateTime(ruleResponse.getUpdatedAt()));

		this.logUtility.logMessage("Rule fetched successfully", INFO, log);
		return new ResponseEntity<>(ruleResponse, HttpStatus.OK);
	}

	@Override
	public ResponseEntity<?> getRulesBasedOnTypeAndPagination(Integer page, Integer size, RuleType ruleType)
			throws Exception {
		String rule = getRuleTypeToPassToZeta(ruleType);
		this.logUtility.logMessage("Converted rule type to appropriate format to pass to zeta: " + rule, INFO, log);
		return getAllRules(page, size, rule);
	}

	private String getErrorMessage(ErrorResponse errorResponse) {
		if (errorResponse == null || errorResponse.getErrorMessage() == null
				|| errorResponse.getErrorMessage().isBlank()) {
			return "Error occurred while processing spend rules request.";
		}
		return errorResponse.getErrorMessage();
	}

	private String convertMilliSecToLocalDateTime(String milliSeconds) {
		try {
			return Instant.ofEpochMilli(Long.valueOf(milliSeconds))
				.atZone(ZoneId.systemDefault())
				.toLocalDateTime()
				.toString();
		}
		catch (Exception ex) {
			this.logUtility.logMessage("Error occurred while converting milliseconds to LocalDateTime ", ERROR, log);
			return "";
		}
	}

	private String getRuleTypeToPassToZeta(RuleType ruleType) {
		if (ruleType == null) {
			return null;
		}
		try {
			if ("GLOBAL".equalsIgnoreCase(ruleType.name())) {
				return "Required";
			}
			if ("CUSTOM".equalsIgnoreCase(ruleType.name())) {
				return "Custom";
			}
			if ("GENERAL".equalsIgnoreCase(ruleType.name())) {
				return "General";
			}
		}
		catch (Exception ex) {
			this.logUtility.logMessage("Error occurred while fetching rules by type: " + ruleType, ERROR, log);
		}
		return null;
	}

}
