package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.producer.StorageQueueProducerApi;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.ResetCardService;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.AUTH_CLEAR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.IN_PROGRESS;

@Slf4j
@Service
public class ResetCardServiceImpl implements ResetCardService {

	private final LogUtility logUtility;

	private final AuthExtractRepository authExtractRepository;

	private final StorageQueueProducerApi storageQueueProducer;

	private final AuditLogger auditLogger;

	private final PaymentRequestRepository paymentRequestRepository;

	public ResetCardServiceImpl(LogUtility logUtility, AuthExtractRepository authExtractRepository,
			StorageQueueProducerApi storageQueueProducer, AuditLogger auditLogger,
			PaymentRequestRepository paymentRequestRepository) {
		this.logUtility = logUtility;
		this.authExtractRepository = authExtractRepository;
		this.storageQueueProducer = storageQueueProducer;
		this.auditLogger = auditLogger;
		this.paymentRequestRepository = paymentRequestRepository;
	}

	@Override
	public void sendResetCardRequest(ResetCardRequest resetCardRequest) {

		PaymentRequest paymentRequest = this.paymentRequestRepository.findByPaymentId(resetCardRequest.getPaymentId());
		if (paymentRequest == null) {
			this.logUtility.logMessage("payment not found for paymentId: " + resetCardRequest.getPaymentId(),
					AppConstants.ERROR, log);
			throw new NotFoundException("payment not found for paymentId: " + resetCardRequest.getPaymentId());
		}

		this.logUtility.logMessage("Sending reset card request message to StorageQueue", AppConstants.DEBUG, log);

		AuthExtract authExtract = this.authExtractRepository.findByReceiptId(resetCardRequest.getReceiptId());

		if (authExtract == null) {
			this.logUtility.logMessage("auth not found for receiptId: " + resetCardRequest.getReceiptId(),
					AppConstants.ERROR, log);
			throw new BadRequestException("auth not found for receiptId: " + resetCardRequest.getReceiptId());
		}
		else {
			if (authExtract.getClearingStatus() != null && (AUTH_CLEAR.equalsIgnoreCase(authExtract.getClearingStatus())
					|| IN_PROGRESS.equalsIgnoreCase(authExtract.getClearingStatus()))) {
				if (AUTH_CLEAR.equalsIgnoreCase(authExtract.getClearingStatus())) {
					this.logUtility.logMessage("Auth cannot be reset as it is already cleared", AppConstants.ERROR,
							log);
					throw new BadRequestException(
							"This auth has already been cleared and cannot be reset for receiptId: "
									+ resetCardRequest.getReceiptId());
				}
				else if (IN_PROGRESS.equalsIgnoreCase(authExtract.getClearingStatus())) {
					this.logUtility.logMessage("Auth reset is already in progress", AppConstants.ERROR, log);
					throw new BadRequestException("A reset is already in progress for this auth (receiptId: "
							+ resetCardRequest.getReceiptId() + "). Please wait until the process completes.");
				}
			}

			this.auditLogger.logResetReceived(null, resetCardRequest.getPaymentId(), resetCardRequest.getReceiptId());
			resetCardRequest.setCardGuid(authExtract.getCardId());
			authExtract.setClearingStatus(IN_PROGRESS);
			this.authExtractRepository.save(authExtract);
			this.storageQueueProducer.sendMessageToQueue(resetCardRequest);
		}
	}

}
