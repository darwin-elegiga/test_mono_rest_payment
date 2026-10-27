package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.organization.events.LogoUpdated;
import com.uhg.optum.financials.payments.payer.exception.UpdateEventsException;
import com.uhg.optum.financials.payments.payer.model.Account;
import com.uhg.optum.financials.payments.payer.model.LogoDetails;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.model.events.Organization;
import com.uhg.optum.financials.payments.payer.repository.AccountRepository;
import com.uhg.optum.financials.payments.payer.repository.LogoDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.OrganizationRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.service.IEventBusHandler;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.client.events.IEvent;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ACCOUNT;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.LOGO_NAME;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.LOGO_PATH;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.SUB_ACCOUNT;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.UPDATE_LOGO_ERROR_LOGS;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;

@Slf4j
@Service("logoServiceEventHandler")
public class LogoServiceEventHandler implements IEventBusHandler {

	@Value("${base.url}")
	private String baseUrl;

	@Autowired
	private LogoDetailsRepository logoDetailsRepository;

	@Autowired
	AccountRepository accountRepository;

	@Autowired
	SubAccountRepository subAccountRepository;

	@Autowired
	OrganizationRepository organizationRegisteredRepository;

	@Autowired
	private LogUtility logUtility;

	@Override
	@SuppressWarnings({ "unchecked" })
	public <T extends IEvent> void handle(EventBusEvent<T> message) {
		log.debug("Received event: {}", message.getPayload().getClass().getSimpleName());
		if (message.getPayload() instanceof LogoUpdated) {
			handleLogoUpdatedEvents((EventBusEvent<LogoUpdated>) message);
		}
	}

	private void handleLogoUpdatedEvents(EventBusEvent<LogoUpdated> event) {
		try {
			LogoUpdated logoUpdated = event.getPayload();
			String orgShortCode = getOrgShortCode(UUID.fromString(event.getId()));
			LogoDetails logoDetails = getLogoDetails(orgShortCode, logoUpdated);
			if (logoUpdated.getAccountShortCode() != null && logoUpdated.getSubAccountShortCode() != null) {
				SubAccount subAccount = this.subAccountRepository
					.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(logoUpdated.getAccountShortCode(),
							logoUpdated.getSubAccountShortCode(), orgShortCode)
					.orElse(null);
				subAccount.setHasCustomLogo(true);
				logoDetails.setAccountShortCode(logoUpdated.getAccountShortCode());
				logoDetails.setSubAccountShortCode(logoUpdated.getSubAccountShortCode());
				logoDetails
					.setLogoUrl(this.baseUrl + LOGO_PATH + orgShortCode + ACCOUNT + logoUpdated.getAccountShortCode()
							+ SUB_ACCOUNT + logoUpdated.getSubAccountShortCode() + LOGO_NAME);
			}
			else if (logoUpdated.getAccountShortCode() != null) {
				Account payer = this.accountRepository
					.findByAccountShortCodeAndOrgShortCode(logoUpdated.getAccountShortCode(), orgShortCode);
				payer.setHasCustomLogo(true);
				logoDetails.setAccountShortCode(logoUpdated.getAccountShortCode());
				logoDetails.setLogoUrl(this.baseUrl + LOGO_PATH + orgShortCode + ACCOUNT
						+ logoUpdated.getAccountShortCode() + LOGO_NAME);
			}
			else {
				Organization organizationRegisteredDetails = this.organizationRegisteredRepository
					.findByOrgShortCode(orgShortCode);
				organizationRegisteredDetails.setHasCustomLogo(true);
				logoDetails.setLogoUrl(this.baseUrl + LOGO_PATH + orgShortCode + LOGO_NAME);
			}
			this.logoDetailsRepository.save(logoDetails);
			this.logUtility.logMessage("saving logo updated event", INFO, log);
		}
		catch (Exception ex) {
			this.logUtility.logMessageWithPriority(UPDATE_LOGO_ERROR_LOGS + ex.getMessage(), ERROR, log, P3);
			throw new UpdateEventsException(ex.getMessage());
		}
	}

	private String getOrgShortCode(UUID orgId) {
		return this.organizationRegisteredRepository.findByOrgId(orgId).getOrgShortCode();
	}

	private LogoDetails getLogoDetails(String orgShortCode, LogoUpdated logoUpdated) {
		LogoDetails logoDetails = this.logoDetailsRepository
			.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(logoUpdated.getAccountShortCode(),
					logoUpdated.getSubAccountShortCode(), orgShortCode);
		if (logoDetails == null) {
			logoDetails = new LogoDetails();
			logoDetails.setOrgShortCode(orgShortCode);
			logoDetails.setCreatedOn(LocalDateTime.now());
			logoDetails.setType(logoUpdated.getType());
			logoDetails.setCreatedById(VIRTUAL_CARD);
			logoDetails.setUpdatedById(logoUpdated.getUpdatedBy());
		}
		else {
			logoDetails.setLastUpdate(LocalDateTime.now());
		}
		return logoDetails;
	}

}
