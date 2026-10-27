package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.logo.LogoEventDto;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.LogoDetails;
import com.uhg.optum.financials.payments.payer.repository.LogoDetailsRepository;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusProducer;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import static com.uhg.optum.financials.payments.payer.constant.Constants.SUCCESS_EVENT;

/**
 * Description: PublishLogoUpdatedEventServiceTests.
 *
 * @author bhemabh, created on 12-August-2024
 */
@ExtendWith(MockitoExtension.class)
public class LogoUpdatedEventServiceTests {

	@InjectMocks
	private LogoUpdatedEventServiceImpl publishLogoUpdatedEventService;

	@Mock
	private LogoDetailsRepository logoDetailsRepository;

	@Mock
	private EventBusProducer producerService;

	private String shortCode = "testCode";

	@Test
	public void testPublishLogoUpdatedEvent() {
		LogoEventDto logoUpdatedEvent = new LogoEventDto();
		String expectedResponse = SUCCESS_EVENT;
		String response = this.publishLogoUpdatedEventService.publishLogoUpdatedEvent(logoUpdatedEvent);
		Assertions.assertThat(response).isEqualTo(expectedResponse);
	}

	@Test
	public void testGetLogoDetailsException() {
		Mockito
			.when(this.logoDetailsRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(Mockito.any(),
					Mockito.any(), Mockito.any()))
			.thenReturn(null);
		Assertions.assertThatException()
			.isThrownBy(() -> this.publishLogoUpdatedEventService.getLogoDetails(this.shortCode, this.shortCode,
					this.shortCode))
			.isInstanceOf(NotFoundException.class);
	}

	@Test
	public void testGetLogoDetails() {
		LogoDetails logoDetails = new LogoDetails();
		Mockito
			.when(this.logoDetailsRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(Mockito.any(),
					Mockito.any(), Mockito.any()))
			.thenReturn(logoDetails);
		Assertions
			.assertThat(
					this.publishLogoUpdatedEventService.getLogoDetails(this.shortCode, this.shortCode, this.shortCode))
			.isEqualTo(logoDetails);

	}

	@Test
	public void testGetLogoDetailsSubAccountNull() {
		LogoDetails logoDetails = new LogoDetails();
		Mockito
			.when(this.logoDetailsRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(Mockito.any(),
					Mockito.any(), Mockito.any()))
			.thenReturn(logoDetails);
		Assertions.assertThat(this.publishLogoUpdatedEventService.getLogoDetails(this.shortCode, null, this.shortCode))
			.isEqualTo(logoDetails);

	}

	@Test
	public void testGetLogoDetailsAccountNull() {
		LogoDetails logoDetails = new LogoDetails();
		Mockito
			.when(this.logoDetailsRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(Mockito.any(),
					Mockito.any(), Mockito.any()))
			.thenReturn(logoDetails);
		Assertions.assertThat(this.publishLogoUpdatedEventService.getLogoDetails(null, null, this.shortCode))
			.isEqualTo(logoDetails);

	}

}
