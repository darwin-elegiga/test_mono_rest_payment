package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.SpendRulesDTO;
import com.uhg.optum.financials.payments.payer.dto.SpendRulesData;
import com.uhg.optum.financials.payments.payer.dto.SpendRulesResponseDTO;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.SpendRules;
import com.uhg.optum.financials.payments.payer.repository.AccountRepository;
import com.uhg.optum.financials.payments.payer.repository.OrganizationRepository;
import com.uhg.optum.financials.payments.payer.repository.SpendRulesConfigRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.service.SpendRulesConfigService;
import com.uhg.optum.financials.payments.payer.util.UserNameExtractUtil;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.lang3.StringUtils;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

@Service
@Slf4j
public class SpendRulesConfigServiceImpl implements SpendRulesConfigService {

	private final SpendRulesConfigRepository spendRulesConfigRepository;

	private final OrganizationRepository organizationRepository;

	private final AccountRepository accountRepository;

	private final SubAccountRepository subAccountRepository;

	@Autowired
	public SpendRulesConfigServiceImpl(SpendRulesConfigRepository spendRulesConfigRepository,
			OrganizationRepository organizationRepository, AccountRepository accountRepository,
			SubAccountRepository subAccountRepository) {
		this.spendRulesConfigRepository = spendRulesConfigRepository;
		this.organizationRepository = organizationRepository;
		this.accountRepository = accountRepository;
		this.subAccountRepository = subAccountRepository;
	}

	@Override
	@Transactional
	public List<SpendRulesResponseDTO> loadSpendRuleConfigs(SpendRulesDTO spendRulesDTO, String orgShortCd) {
		validateOrgShortCd(orgShortCd);

		List<SpendRulesResponseDTO> responseList = new ArrayList<>();
		if (spendRulesDTO.getAccounts() == null || spendRulesDTO.getAccounts().isEmpty()) {
			responseList.add(processSpendRuleConfig(spendRulesDTO, orgShortCd, null, null, false));
		}
		else {
			spendRulesDTO.getAccounts().forEach((account) -> {
				if (account.getSubAccounts() != null && !account.getSubAccounts().isEmpty()) {
					account.getSubAccounts()
						.forEach((subAccShortCd) -> responseList.add(processSpendRuleConfig(spendRulesDTO, orgShortCd,
								account.getAccShortCd(), subAccShortCd, false)));
				}
				else {
					responseList
						.add(processSpendRuleConfig(spendRulesDTO, orgShortCd, account.getAccShortCd(), null, false));
				}
			});
		}
		return responseList;
	}

	private SpendRulesResponseDTO processSpendRuleConfig(SpendRulesDTO spendRulesDTO, String orgShortCd,
			String accountShortCd, String subAccountShortCd, boolean isUpdate) {
		Optional<SpendRules> existingRules = spendRulesValidateAndExistenceCheck(spendRulesDTO, orgShortCd,
				accountShortCd, subAccountShortCd);

		if (existingRules.isPresent() && isUpdate) {
			updateSpendRules(spendRulesDTO, existingRules, orgShortCd, accountShortCd, subAccountShortCd);
			return buildResponse("Spend validation rules updated successfully.", spendRulesDTO, orgShortCd,
					accountShortCd, subAccountShortCd);
		}
		if (existingRules.isPresent() && !isUpdate) {
			throw new BadRequestException(
					"Spend rules for this combination already exist and should not be created again.");
		}
		else {
			if (isUpdate) {
				throw new NotFoundException("No existing spend rules found for the provided organization details.");
			}
			saveSpendRulesConfig(spendRulesDTO, orgShortCd, accountShortCd, subAccountShortCd);
			return buildResponse("Spend validation rules configured successfully.", spendRulesDTO, orgShortCd,
					accountShortCd, subAccountShortCd);
		}
	}

	@Transactional
	private void saveSpendRulesConfig(SpendRulesDTO spendRulesDTO, String orgShortCd, String accountShortCd,
			String subAccountShortCd) {
		List<String> names = this.organizationRepository
			.getOrgAccountSubAccountNames(orgShortCd, accountShortCd, subAccountShortCd)
			.getFirst();
		SpendRules spendRulesConfig = new SpendRules();
		spendRulesConfig.setOrgShortCode(orgShortCd);
		spendRulesConfig.setOrgName(getNullIfEmpty(names.get(0)));
		spendRulesConfig.setAccountName(getNullIfEmpty(names.get(1)));
		spendRulesConfig.setSubAccountName(getNullIfEmpty(names.get(2)));
		spendRulesConfig.setAccountShortCode(getNullIfEmpty(accountShortCd));
		spendRulesConfig.setSubAccountShortCode(getNullIfEmpty(subAccountShortCd));
		spendRulesConfig.setRuleCode(spendRulesDTO.getSpendRules());
		spendRulesConfig.setCreatedBy(UserNameExtractUtil.getUserNameFromToken());
		this.spendRulesConfigRepository.save(spendRulesConfig);
		log.info("Spend rules configuration saved successfully.");
	}

	@Override
	@Transactional
	public List<SpendRulesResponseDTO> updateSpendRuleConfigs(SpendRulesDTO spendRulesDTO, String orgShortCd) {
		validateOrgShortCd(orgShortCd);

		List<SpendRulesResponseDTO> responseList = new ArrayList<>();
		if (spendRulesDTO.getAccounts() == null || spendRulesDTO.getAccounts().isEmpty()) {
			responseList.add(processSpendRuleConfig(spendRulesDTO, orgShortCd, null, null, true));
		}
		else {
			spendRulesDTO.getAccounts().forEach((account) -> {
				if (account.getSubAccounts() != null && !account.getSubAccounts().isEmpty()) {
					account.getSubAccounts().forEach((subAccShortCd) -> {
						Optional<SpendRules> existingRule = spendRulesValidateAndExistenceCheck(spendRulesDTO,
								orgShortCd, account.getAccShortCd(), subAccShortCd);
						if (existingRule.isPresent()) {
							updateSpendRules(spendRulesDTO, existingRule, orgShortCd, account.getAccShortCd(),
									subAccShortCd);
							responseList.add(buildResponse("Spend validation rules updated successfully.",
									spendRulesDTO, orgShortCd, account.getAccShortCd(), subAccShortCd));
						}
						else {
							log.error("Spend rules configuration not found");
							throw new NotFoundException(
									"No existing spend rules found for the provided organization, account and subAccount details.");
						}
					});
				}
				else {
					Optional<SpendRules> existingRule = spendRulesValidateAndExistenceCheck(spendRulesDTO, orgShortCd,
							account.getAccShortCd(), null);
					if (existingRule.isPresent()) {
						updateSpendRules(spendRulesDTO, existingRule, orgShortCd, account.getAccShortCd(), null);
						responseList.add(buildResponse("Spend validation rules updated successfully.", spendRulesDTO,
								orgShortCd, account.getAccShortCd(), null));
					}
					else {
						log.error("Spend rules configuration not found");
						throw new NotFoundException(
								"No existing spend rules found for the provided organization and account details.");
					}
				}
			});
		}
		return responseList;
	}

	@Transactional
	private void updateSpendRules(SpendRulesDTO spendRulesDTO, Optional<SpendRules> existingRule, String orgShortCd,
			String accountShortCd, String subAccountShortCd) {
		SpendRules spendRulesConfig = existingRule
			.orElseThrow(() -> new NotFoundException("Spend rules not found for update."));
		spendRulesConfig.setRuleCode(spendRulesDTO.getSpendRules());
		spendRulesConfig.setUpdatedAt(LocalDateTime.now());
		spendRulesConfig.setUpdatedBy(UserNameExtractUtil.getUserNameFromToken());
		this.spendRulesConfigRepository.save(spendRulesConfig);
		log.info("Spend rules configuration updated successfully.");
	}

	private Optional<SpendRules> spendRulesValidateAndExistenceCheck(SpendRulesDTO spendRulesDTO, String orgShortCd,
			String accountShortCd, String subAccountShortCd) {
		validateAccountAndSubAccount(accountShortCd, subAccountShortCd);
		if (spendRulesDTO != null) {
			validateSpendRules(spendRulesDTO);
		}
		List<Boolean> result = this.organizationRepository
			.checkOrgAccountSubAccountExistence(orgShortCd, accountShortCd, subAccountShortCd)
			.getFirst();
		validateExistence(result, accountShortCd, subAccountShortCd);
		return findExistingSpendRules(orgShortCd, accountShortCd, subAccountShortCd);
	}

	private void validateOrgShortCd(String orgShortCd) {
		if (StringUtils.isBlank(orgShortCd)) {
			log.error("Organization short code is missing.");
			throw new IllegalArgumentException("Organization short code cannot be null or empty.");
		}
	}

	private void validateAccountAndSubAccount(String accountShortCd, String subAccountShortCd) {
		if (StringUtils.isNotBlank(subAccountShortCd) && StringUtils.isBlank(accountShortCd)) {
			log.error("Account short code is required when sub-account short code is provided.");
			throw new BadRequestException(
					"Account short code cannot be null or empty when sub-account short code is provided.");
		}
	}

	private void validateSpendRules(SpendRulesDTO spendRulesDTO) {
		if (spendRulesDTO.getSpendRules() == null || spendRulesDTO.getSpendRules().isEmpty()) {
			log.error("Spend rules cannot be null or empty.");
			throw new BadRequestException("Spend rules cannot be null or empty.");
		}
	}

	private void validateExistence(List<Boolean> result, String accountShortCd, String subAccountShortCd) {
		boolean orgExists = result.get(0);
		boolean accountExists = result.get(1);
		boolean subAccountExists = result.get(2);

		if (!orgExists) {
			throw new NotFoundException("Organization with the provided short code does not exist.");
		}
		if (StringUtils.isNotBlank(accountShortCd) && !accountExists) {
			throw new NotFoundException("Account with the provided short code does not exist for the organization.");
		}
		if (StringUtils.isNotBlank(subAccountShortCd) && !subAccountExists) {
			throw new NotFoundException(
					"Sub-account with the provided short code does not exist for the organization and account.");
		}
	}

	private Optional<SpendRules> findExistingSpendRules(String orgShortCd, String accountShortCd,
			String subAccountShortCd) {
		if (StringUtils.isNotBlank(subAccountShortCd)) {
			return this.spendRulesConfigRepository.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode(
					orgShortCd, accountShortCd, subAccountShortCd);
		}
		else if (StringUtils.isNotBlank(accountShortCd)) {
			return this.spendRulesConfigRepository
				.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCodeIsNull(orgShortCd, accountShortCd);
		}
		else {
			return this.spendRulesConfigRepository
				.findByOrgShortCodeAndAccountShortCodeIsNullAndSubAccountShortCodeIsNull(orgShortCd);
		}
	}

	private SpendRulesResponseDTO buildResponse(String message, SpendRulesDTO spendRulesDTO, String orgShortCd,
			String accountShortCd, String subAccountShortCd) {
		SpendRulesResponseDTO response = new SpendRulesResponseDTO();
		response.setMessage(message);
		response.setData(buildSpendRulesData(spendRulesDTO, orgShortCd, accountShortCd, subAccountShortCd));
		return response;
	}

	private SpendRulesData buildSpendRulesData(SpendRulesDTO spendRulesDTO, String orgShortCd, String accountShortCd,
			String subAccountShortCd) {
		SpendRulesData data = new SpendRulesData();
		data.setOrgShortCd(orgShortCd);
		data.setAccShortCd(accountShortCd);
		data.setSubAccShortCd(subAccountShortCd);
		data.setSpendRules(spendRulesDTO.getSpendRules());
		data.setCreatedBy(spendRulesDTO.getCreatedBy());
		data.setLastUpdatedBy(spendRulesDTO.getUpdatedBy());
		return data;
	}

	private String getNullIfEmpty(String value) {
		return StringUtils.isBlank(value) ? null : value;
	}

	@Override
	public SpendRulesResponseDTO getSpendRuleConfigs(String orgShortCd, String accShortCd, String subAccShortCd) {
		Optional<SpendRules> existSpendRules = spendRulesValidateAndExistenceCheck(null, orgShortCd, accShortCd,
				subAccShortCd);
		if (existSpendRules.isPresent()) {
			SpendRulesResponseDTO response = new SpendRulesResponseDTO();
			response.setMessage("Spend validation rules retrieved successfully.");
			response.setData(buildSpendRulesDataFromEntity(existSpendRules.get()));
			return response;
		}
		else {
			log.error(
					"No spend rules configuration found for the provided organization, account and sub-account details.");
			throw new NotFoundException(
					"No spend rules configuration found for the provided organization, account and sub-account details.");
		}
	}

	@Override
	public List<SpendRules> getAllSpendRuleConfigs() {
		List<SpendRules> spendRules = this.spendRulesConfigRepository.findAll();
		if (spendRules == null || spendRules.isEmpty()) {
			throw new NotFoundException("No spend rules configurations found.");
		}
		return spendRules;
	}

	@Override
	@Transactional
	public SpendRulesResponseDTO deleteSpendRuleConfigs(String orgShortCd, String accShortCd, String subAccShortCd) {
		Optional<SpendRules> existSpendRules = spendRulesValidateAndExistenceCheck(null, orgShortCd, accShortCd,
				subAccShortCd);
		if (existSpendRules.isPresent()) {
			this.spendRulesConfigRepository.delete(existSpendRules.get());
			log.info("Spend rules configuration deleted successfully.");
			SpendRulesResponseDTO response = new SpendRulesResponseDTO();
			response.setMessage("Spend rules configuration deleted successfully.");
			return response;
		}
		else {
			log.error("No spend rules configuration found for the provided details.");
			throw new NotFoundException(
					"No spend rules configuration found for the provided organization, account and sub-account details.");
		}
	}

	private SpendRulesData buildSpendRulesDataFromEntity(SpendRules spendRules) {
		SpendRulesData data = new SpendRulesData();
		data.setOrgShortCd(spendRules.getOrgShortCode());
		data.setOrgName(spendRules.getOrgName());
		data.setAcctName(spendRules.getAccountName());
		data.setSubAcctName(spendRules.getSubAccountName());
		data.setAccShortCd(spendRules.getAccountShortCode());
		data.setSubAccShortCd(spendRules.getSubAccountShortCode());
		data.setSpendRules(spendRules.getRuleCode());
		data.setCreatedBy(spendRules.getCreatedBy());
		data.setCreatedDate(String.valueOf(spendRules.getCreatedAt()));
		data.setLastUpdatedBy(spendRules.getUpdatedBy());
		data.setLastUpdatedDate(String.valueOf(spendRules.getUpdatedAt()));
		return data;
	}

}
