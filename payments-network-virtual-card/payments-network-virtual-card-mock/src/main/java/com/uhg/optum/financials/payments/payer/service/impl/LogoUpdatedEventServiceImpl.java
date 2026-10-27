package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.organization.events.LogoUpdated;
import com.uhg.optum.financials.payments.payer.dto.logo.LogoEventDto;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.LogoDetails;
import com.uhg.optum.financials.payments.payer.repository.LogoDetailsRepository;
import com.uhg.optum.financials.payments.payer.service.LogoUpdatedEventService;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusProducer;
import lombok.NoArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.stereotype.Service;

import java.time.Instant;

import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.LOGO_UPDATED;

@Service
@Slf4j
@NoArgsConstructor
public class LogoUpdatedEventServiceImpl implements LogoUpdatedEventService {

	@Autowired
	@Qualifier("kafkaEventBusProducer")
	private EventBusProducer producerService;

	@Autowired
	private LogoDetailsRepository logoDetailsRepository;

	public String publishLogoUpdatedEvent(LogoEventDto logoEventDto) {

		try {
			log.info("Sending message to event hub");
			EventBusEvent<LogoUpdated> event = convertLogoUpdatedDtoToEventMessage(logoEventDto);
			this.producerService.sendEvent(event, null);
			return "SUCCESS";
		}
		catch (Exception ex) {
			log.error("Exception occurred while sending message to event bus :{}", ex.getMessage());
			return "FAILED";
		}
	}

	private EventBusEvent<LogoUpdated> convertLogoUpdatedDtoToEventMessage(LogoEventDto logoEventDto) {
		EventBusEvent<LogoUpdated> eventMessage = new EventBusEvent<>();
		eventMessage.setId(String.valueOf(logoEventDto.getId()));
		eventMessage.setTenantId(String.valueOf(logoEventDto.getTenantId()));
		eventMessage.setEventId(logoEventDto.getEventId());
		eventMessage.setPayload(logoEventDto.getLogoUpdated());
		eventMessage.setType(LOGO_UPDATED);
		eventMessage.setCreatedOn(Instant.now());
		return eventMessage;
	}

	@Override
	public LogoDetails getLogoDetails(String accountShortCode, String subAccountShortCode, String orgShortCode) {
		LogoDetails logoDetails = null;
		if (subAccountShortCode != null && accountShortCode != null) {
			logoDetails = this.logoDetailsRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(
					accountShortCode, subAccountShortCode, orgShortCode);
		}
		else if (accountShortCode != null && subAccountShortCode == null) {
			logoDetails = this.logoDetailsRepository
				.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(accountShortCode, null, orgShortCode);
		}
		else if (orgShortCode != null) {
			logoDetails = this.logoDetailsRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(null,
					null, orgShortCode);
		}
		if (logoDetails != null) {
			return logoDetails;
		}
		else {
			String sanitizedAccountShortCode = accountShortCode.replace("\n", "").replace("\r", "");
			log.info("logo details not found {}", sanitizedAccountShortCode);
			throw new NotFoundException("logo details not found " + accountShortCode + " " + subAccountShortCode);
		}
	}

}
