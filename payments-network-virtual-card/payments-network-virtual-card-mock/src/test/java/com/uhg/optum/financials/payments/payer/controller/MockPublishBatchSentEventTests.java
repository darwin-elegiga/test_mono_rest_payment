package com.uhg.optum.financials.payments.payer.controller;

import static org.assertj.core.api.BDDAssertions.then;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.verify;

import com.uhg.optum.financials.payments.payer.dto.ResponseDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchEventDto;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.service.BatchEventPublishService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.ResponseEntity;

@ExtendWith(MockitoExtension.class)
class MockPublishBatchSentEventTests {

	@Mock
	private BatchEventPublishService batchEventPublishService;

	@Mock
	private LogUtility logUtility;

	@InjectMocks
	private MockPublishBatchSentEvent mockPublishBatchSentEvent;

	@Test
	void testPublishBatchEventReturnsOk() {
		BatchEventDto batchEventDto = new BatchEventDto();
		CorrelationDto correlationDto = new CorrelationDto();
		correlationDto.setOrg("ORG1");
		batchEventDto.setCorrelationDto(correlationDto);
		String publishResult = "published";
		given(this.batchEventPublishService.publishBatchEvent(batchEventDto)).willReturn(publishResult);

		ResponseEntity<ResponseDto<String>> response = this.mockPublishBatchSentEvent.publishBatchEvent(batchEventDto);

		then(response.getStatusCode().value()).isEqualTo(200);
		then(response.getBody().getMessage()).isEqualTo(publishResult);
		then(response.getBody().getPayload()).isEqualTo("ORG1");
		then(response.getBody().isStatus()).isTrue();
		verify(this.logUtility).logMessage("Sending batch event message to event bus",
				com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO,
				org.slf4j.LoggerFactory.getLogger(MockPublishBatchSentEvent.class));
	}

	@Test
	void testPublishBatchEventWithNullCorrelationDto() {
		BatchEventDto batchEventDto = new BatchEventDto();
		batchEventDto.setCorrelationDto(null);
		given(this.batchEventPublishService.publishBatchEvent(batchEventDto)).willReturn("published");
		ResponseEntity<ResponseDto<String>> response = this.mockPublishBatchSentEvent.publishBatchEvent(batchEventDto);
		then(response.getBody().getMessage()).isEqualTo("published");
		then(response.getBody().getPayload()).isNull();
		then(response.getBody().isStatus()).isTrue();
	}

}
