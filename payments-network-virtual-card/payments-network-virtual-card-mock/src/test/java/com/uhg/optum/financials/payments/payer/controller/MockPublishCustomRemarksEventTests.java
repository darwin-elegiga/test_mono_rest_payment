package com.uhg.optum.financials.payments.payer.controller;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;

import java.util.UUID;

import com.uhg.optum.financials.payments.payer.dto.CustomRemarkUpdatedEventDto;
import com.uhg.optum.financials.payments.payer.dto.ResponseDto;
import com.uhg.optum.financials.payments.payer.model.DocumentCustomRemarks;
import com.uhg.optum.financials.payments.payer.repository.DocumentCustomRemarksRepository;
import com.uhg.optum.financials.payments.payer.service.CustomRemarksPublisherService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

@ExtendWith(MockitoExtension.class)
class MockPublishCustomRemarksEventTests {

	@InjectMocks
	private MockPublishCustomRemarksEvent controller;

	@Mock
	private CustomRemarksPublisherService customRemarksPublisherService;

	@Mock
	private LogUtility logUtility;

	@Mock
	private DocumentCustomRemarksRepository documentCustomRemarksRepository;

	private CustomRemarkUpdatedEventDto dto;

	@BeforeEach
	void setUp() {
		this.dto = new CustomRemarkUpdatedEventDto();
		this.dto.setId(UUID.fromString("123e4567-e89b-12d3-a456-426614174000"));
		this.dto.setTenantId(UUID.fromString("123e4567-e89b-12d3-a456-426614174001"));
		this.dto.setEventId(UUID.fromString("123e4567-e89b-12d3-a456-426614174002"));
	}

	// ─── sendCustomRemarksUpdatedEvent ───────────────────────────────────────

	@Test
	void sendCustomRemarksUpdatedEvent_returnsOkWithServiceResult() {
		given(this.customRemarksPublisherService.publishCustomRemarksUpdatedEvent(this.dto)).willReturn("SUCCESS");

		ResponseEntity<ResponseDto<String>> response = this.controller.sendCustomRemarksUpdatedEvent(this.dto);

		assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		assertThat(response.getBody()).isNotNull();
		assertThat(response.getBody().getMessage()).isEqualTo("SUCCESS");
		assertThat(response.getBody().isStatus()).isTrue();
		assertThat(response.getBody().getPayload()).isEqualTo(this.dto.getId().toString());
		then(this.customRemarksPublisherService).should().publishCustomRemarksUpdatedEvent(this.dto);
	}

	@Test
	void sendCustomRemarksUpdatedEvent_propagatesServiceReturnValue() {
		given(this.customRemarksPublisherService.publishCustomRemarksUpdatedEvent(any())).willReturn("EVENT_PUBLISHED");

		ResponseEntity<ResponseDto<String>> response = this.controller.sendCustomRemarksUpdatedEvent(this.dto);

		assertThat(response.getBody()).isNotNull();
		assertThat(response.getBody().getMessage()).isEqualTo("EVENT_PUBLISHED");
	}

	// ─── getCustomRemarks ────────────────────────────────────────────────────

	@Test
	void getCustomRemarks_returnsOkWithMatchingRecord() {
		DocumentCustomRemarks remarks = new DocumentCustomRemarks();
		remarks.setOrgShortCode("ORG01");
		remarks.setAccountShortCode("ACC01");
		remarks.setSubAccountShortCode("SUB01");
		given(this.documentCustomRemarksRepository.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode("ORG01",
				"ACC01", "SUB01"))
			.willReturn(remarks);

		ResponseEntity<DocumentCustomRemarks> response = this.controller.getCustomRemarks("ORG01", "ACC01", "SUB01");

		assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		assertThat(response.getBody()).isEqualTo(remarks);
		then(this.documentCustomRemarksRepository).should()
			.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode("ORG01", "ACC01", "SUB01");
	}

	@Test
	void getCustomRemarks_returnsNullBodyWhenNotFound() {
		given(this.documentCustomRemarksRepository.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode(any(),
				any(), any()))
			.willReturn(null);

		ResponseEntity<DocumentCustomRemarks> response = this.controller.getCustomRemarks("ORG01", "ACC01", "SUB01");

		assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		assertThat(response.getBody()).isNull();
	}

}
