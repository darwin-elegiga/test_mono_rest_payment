package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.dto.CancelCardDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.producer.StorageQueueProducerApi;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.CancelCardService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.util.UUID;

@Slf4j
@Service
public class CancelCardServiceImpl implements CancelCardService {

	private final LogUtility logUtility;

	private final VirtualCardsRepository virtualCardsRepository;

	private final StorageQueueProducerApi storageQueueProducer;

	public CancelCardServiceImpl(LogUtility logUtility, VirtualCardsRepository virtualCardsRepository,
			StorageQueueProducerApi storageQueueProducer) {
		this.logUtility = logUtility;
		this.virtualCardsRepository = virtualCardsRepository;
		this.storageQueueProducer = storageQueueProducer;
	}

	@Override
	public void sendCancelCardRequest(CancelCardDto cancelCardDto) {

		this.logUtility.logMessage("Sending cancel card request message to StorageQueue", AppConstants.DEBUG, log);

		UUID cardId = this.virtualCardsRepository.findCardGuidByPaymentId(cancelCardDto.getPaymentId());

		if (cardId == null) {
			this.logUtility.logMessage("Card not found for paymentId: " + cancelCardDto.getPaymentId(),
					AppConstants.ERROR, log);
			throw new BadRequestException("Card not found for paymentId: " + cancelCardDto.getPaymentId());
		}
		else {
			cancelCardDto.setCardGuid(cardId);
			this.storageQueueProducer.sendMessageToQueue(cancelCardDto);
		}
	}

}
