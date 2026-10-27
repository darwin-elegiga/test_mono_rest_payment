package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.LogoDetailsRequest;
import com.uhg.optum.financials.payments.payer.dto.ResponseDto;
import com.uhg.optum.financials.payments.payer.dto.logo.LogoEventDto;
import com.uhg.optum.financials.payments.payer.model.LogoDetails;
import com.uhg.optum.financials.payments.payer.service.impl.LogoUpdatedEventServiceImpl;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.Constants.SUCCESS;
import static org.mockito.ArgumentMatchers.any;

/**
 * Description: MockLogoUpdatedEventTests.
 *
 * @author bhemabh, created on 12-August-2024
 */
@ExtendWith(MockitoExtension.class)
public class MockPublishLogoUpdatedEventTests {

	@InjectMocks
	private MockPublishLogoUpdatedEvent mockPublishLogoUpdatedEvent;

	@Mock
	private LogoUpdatedEventServiceImpl publishLogoUpdatedEventService;

	@Test
	public void testGetLogoDetails() {
		LogoDetails logoDetails = new LogoDetails();
		LogoDetailsRequest request = new LogoDetailsRequest();
		BDDMockito.when(this.publishLogoUpdatedEventService.getLogoDetails(any(), any(), any()))
			.thenReturn(logoDetails);
		ResponseEntity<?> response = this.mockPublishLogoUpdatedEvent.getLogoDetails(request);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
	}

	@Test
	public void testGetLogoDetailsException() {
		LogoDetailsRequest request = new LogoDetailsRequest();
		BDDMockito.when(this.publishLogoUpdatedEventService.getLogoDetails(any(), any(), any()))
			.thenThrow(new RuntimeException());
		ResponseEntity<?> response = this.mockPublishLogoUpdatedEvent.getLogoDetails(request);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.NOT_FOUND);
	}

	@Test
	public void testSendLogoUpdatedEvent() {
		LogoEventDto logoEventDto = new LogoEventDto();
		logoEventDto.setId(UUID.randomUUID());
		BDDMockito.when(this.publishLogoUpdatedEventService.publishLogoUpdatedEvent(logoEventDto)).thenReturn(SUCCESS);
		ResponseEntity<ResponseDto<String>> response = this.mockPublishLogoUpdatedEvent
			.sendLogoUpdatedEvent(logoEventDto);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
	}

}
