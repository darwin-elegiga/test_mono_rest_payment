package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.CancelCardDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.producer.StorageQueueProducerApi;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.UUID;

import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
public class CancelCardServiceImplTests {

	@InjectMocks
	CancelCardServiceImpl cancelCardService;

	@Mock
	VirtualCardsRepository virtualCardsRepository;

	@Mock
	LogUtility logUtility;

	@Mock
	StorageQueueProducerApi storageQueueProducer;

	@Test
	void testCancelCardException() {
		CancelCardDto cancelCardDto = new CancelCardDto();
		Assertions.assertThatThrownBy(() -> this.cancelCardService.sendCancelCardRequest(cancelCardDto))
			.isInstanceOf(BadRequestException.class);
	}

	@Test
	void testCancelCard() {
		CancelCardDto cancelCardDto = new CancelCardDto();
		BDDMockito.when(this.virtualCardsRepository.findCardGuidByPaymentId(any())).thenReturn(UUID.randomUUID());
		this.cancelCardService.sendCancelCardRequest(cancelCardDto);
	}

}
