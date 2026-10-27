package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.service.StorageQueueProducer;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CORRELATION_EVENT_EXCEPTION_LOG;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CORRELATION_MSG_EXCEPTION_LOG;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.BDDMockito.then;
import static org.mockito.BDDMockito.willThrow;
import static org.mockito.Mockito.never;

@ExtendWith(MockitoExtension.class)
class CorrelationEventPublisherImplTests {

	@Mock
	private StorageQueueProducer storageQueueProducer;

	@Mock
	private LogUtility logUtility;

	@InjectMocks
	private CorrelationEventPublisherImpl correlationEventPublisher;

	@Test
	void publishCorrelationEvent_sendsEventToQueueWhenProducerSucceeds() {
		CorrelationDto correlationDto = new CorrelationDto();
		correlationDto.setCorrelationId("corr-123");

		this.correlationEventPublisher.publishCorrelationEvent(correlationDto);

		then(this.storageQueueProducer).should().sendCorrelationEventToQueue(correlationDto);
		then(this.logUtility).should(never()).logMessageWithPriority(any(), any(), any(), any());
	}

	@Test
	void publishCorrelationEvent_logsAndThrowsBadRequestWhenProducerFails() {
		CorrelationDto correlationDto = new CorrelationDto();
		correlationDto.setCorrelationId("corr-123");
		RuntimeException exception = new RuntimeException("queue unavailable");
		willThrow(exception).given(this.storageQueueProducer).sendCorrelationEventToQueue(correlationDto);

		assertThatThrownBy(() -> this.correlationEventPublisher.publishCorrelationEvent(correlationDto))
			.isInstanceOf(BadRequestException.class)
			.hasMessage(CORRELATION_EVENT_EXCEPTION_LOG + exception.getMessage());

		then(this.logUtility).should()
			.logMessageWithPriority(eq(CORRELATION_MSG_EXCEPTION_LOG + exception.getMessage() + " Correlation Id:"
					+ correlationDto.getCorrelationId()), eq(ERROR), any(), eq(P3));
	}

}
