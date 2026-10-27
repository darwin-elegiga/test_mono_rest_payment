package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.constant.ValidationConstants;
import com.uhg.optum.financials.payments.payer.dto.FileTrackingDto;
import com.uhg.optum.financials.payments.payer.dto.PaymentDetailsResponseDto;
import com.uhg.optum.financials.payments.payer.dto.PaymentResponseDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.exception.QueuePublishException;
import com.uhg.optum.financials.payments.payer.exception.ServiceUnavailableException;
import com.uhg.optum.financials.payments.payer.model.FileTracking;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.repository.FileTrackingRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.LoadPaymentCardCallbackService;
import com.uhg.optum.financials.payments.payer.service.PaymentService;
import com.uhg.optum.financials.payments.payer.service.StorageQueueProducer;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.PaymentRequestUtil;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.BeanUtils;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;

import java.util.Optional;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.PAYMENT_DETAILS_NOTFOUND_LOG;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.PAYMENT_LOAD_EXCEPTION_LOG;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.PAYMENT_MSG_EXCEPTION_LOG;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;

@Slf4j
@Service
public class PaymentServiceImpl implements PaymentService {

	private final PaymentRequestUtil paymentRequestUtil;

	private final LogUtility logUtility;

	private final StorageQueueProducer storageQueueProducer;

	private final PaymentRequestRepository paymentRequestRepository;

	private final FileTrackingRepository fileTrackingRepository;

	private final LoadPaymentCardCallbackService loadPaymentCardCallbackService;

	public PaymentServiceImpl(PaymentRequestUtil paymentRequestUtil, LogUtility logUtility,
			StorageQueueProducer storageQueueProducer, PaymentRequestRepository paymentRequestRepository,
			FileTrackingRepository fileTrackingRepository,
			LoadPaymentCardCallbackService loadPaymentCardCallbackService) {
		this.paymentRequestUtil = paymentRequestUtil;
		this.logUtility = logUtility;
		this.storageQueueProducer = storageQueueProducer;
		this.paymentRequestRepository = paymentRequestRepository;
		this.fileTrackingRepository = fileTrackingRepository;
		this.loadPaymentCardCallbackService = loadPaymentCardCallbackService;
	}

	@Override
	public void loadPayment(PaymentRequestDto paymentRequestDto) {
		this.paymentRequestUtil.checkDuplicatePayment(paymentRequestDto);

		this.paymentRequestUtil.checkOrgAccountSubAccountExistence(paymentRequestDto);

		this.paymentRequestUtil.validateMandatoryFields(paymentRequestDto);
		this.logUtility.logMessage("Sending payment request message to StorageQueue", AppConstants.DEBUG, log);

		try {
			this.storageQueueProducer.sendPaymentEventToQueue(paymentRequestDto);
			this.loadPaymentCardCallbackService.pollAndCallback(paymentRequestDto.getPaymentDetails().getId());
		}
		catch (QueuePublishException ex) {
			log.error("Exception occurred while loading payment and sending incident notification: {}",
					ex.getMessage());
			this.logUtility.logMessageWithPriority(PAYMENT_MSG_EXCEPTION_LOG + ex.getMessage() + " Payment Id:"
					+ paymentRequestDto.getPaymentDetails().getId(), ValidationConstants.ERROR, log, P3);
			throw new ServiceUnavailableException(PAYMENT_LOAD_EXCEPTION_LOG + ex.getMessage(), ex);
		}
		catch (Exception ex) {
			log.error("Exception occurred while loading payment and sending incident notification: {}",
					ex.getMessage());
			this.logUtility.logMessageWithPriority(PAYMENT_MSG_EXCEPTION_LOG + ex.getMessage() + " Payment Id:"
					+ paymentRequestDto.getPaymentDetails().getId(), ValidationConstants.ERROR, log, P3);
			throw new BadRequestException(PAYMENT_LOAD_EXCEPTION_LOG + ex.getMessage());
		}
	}

	@Override
	public PaymentResponseDto getPaymentRequestDetail(String paymentID) {
		PaymentRequest paymentRequest = this.paymentRequestRepository.findByPaymentId(paymentID);
		if (paymentRequest == null) {
			this.logUtility.logMessage(PAYMENT_DETAILS_NOTFOUND_LOG, ERROR, log);
			throw new BadRequestException(PAYMENT_DETAILS_NOTFOUND_LOG + paymentID);
		}
		PaymentRequestDto paymentRequestDto = paymentRequest.toDto();
		PaymentResponseDto paymentResponseDto = new PaymentResponseDto();
		paymentResponseDto.setId(paymentRequest.getId());
		paymentResponseDto.setPaymentDetails(this.buildPaymentDetailsResponse(paymentRequest, paymentRequestDto));
		paymentResponseDto.setPayeeDetails(paymentRequestDto.getPayeeDetails());
		paymentResponseDto.setClaims(paymentRequestDto.getClaims());
		paymentResponseDto.setShippingDetails(paymentRequestDto.getShippingDetails());
		paymentResponseDto.setAccountShortCode(paymentRequestDto.getAccountShortCode());
		paymentResponseDto.setSubAccountShortCode(paymentRequestDto.getSubAccountShortCode());
		paymentResponseDto.setDocuments(paymentRequestDto.getDocuments());
		paymentResponseDto.setOrgShortCode(paymentRequestDto.getOrgShortCode());
		paymentResponseDto.setOrigin(paymentRequestDto.getOrigin());
		paymentResponseDto.setPaymentStatus(paymentRequest.getStatus());
		paymentResponseDto.setSubAccountShortCode(paymentRequestDto.getSubAccountShortCode());
		paymentResponseDto.setMetadata(paymentRequestDto.getMetadata());
		paymentResponseDto.setCorrelationId(paymentRequestDto.getCorrelationId());
		paymentResponseDto.setStSeSegment(paymentRequest.getStSeSegment());
		paymentResponseDto.setRePreferenced(Boolean.TRUE.equals(paymentRequestDto.isRePreferenced()));
		paymentResponseDto.setErrorMessage(paymentRequest.getErrorMessage());
		return paymentResponseDto;
	}

	private PaymentDetailsResponseDto buildPaymentDetailsResponse(PaymentRequest paymentRequest,
			PaymentRequestDto paymentRequestDto) {
		if (paymentRequestDto.getPaymentDetails() == null) {
			return null;
		}

		PaymentDetailsResponseDto paymentDetailsResponseDto = new PaymentDetailsResponseDto();
		BeanUtils.copyProperties(paymentRequestDto.getPaymentDetails(), paymentDetailsResponseDto);
		if (paymentRequest.getPaymentDetails() != null) {
			paymentDetailsResponseDto.setNetworkPaymentId(paymentRequest.getPaymentDetails().getNetworkPaymentId());
		}
		return paymentDetailsResponseDto;
	}

	@Override
	public void reProcessPayment(PaymentRequestDto paymentRequestDto) {
		this.paymentRequestUtil.checkPaymentForReprocessing(paymentRequestDto);

		this.paymentRequestUtil.checkOrgAccountSubAccountExistence(paymentRequestDto);

		this.paymentRequestUtil.validateMandatoryFields(paymentRequestDto);
		this.logUtility.logMessage("Sending payment request message to StorageQueue", AppConstants.DEBUG, log);

		try {
			this.storageQueueProducer.sendPaymentEventToQueue(paymentRequestDto);
		}
		catch (QueuePublishException ex) {
			this.logUtility.logMessageWithPriority(PAYMENT_MSG_EXCEPTION_LOG + ex.getMessage() + " Payment Id:"
					+ paymentRequestDto.getPaymentDetails().getId(), ValidationConstants.ERROR, log, P3);
			throw new ServiceUnavailableException(PAYMENT_LOAD_EXCEPTION_LOG + ex.getMessage(), ex);
		}
		catch (Exception ex) {
			this.recordQueueTransmissionFailure(paymentRequestDto.getPaymentDetails().getId());
			throw new BadRequestException(PAYMENT_LOAD_EXCEPTION_LOG + ex.getMessage());
		}
	}

	private void recordQueueTransmissionFailure(String paymentId) {
		PaymentRequest paymentRequest = this.paymentRequestRepository.findByPaymentId(paymentId);
		if (paymentRequest != null) {
			paymentRequest.setErrorMessage(appendErrorMessage(paymentRequest.getErrorMessage(),
					"Exception while sending the payment message to the queue"));
			this.paymentRequestRepository.save(paymentRequest);
		}
	}

	private String appendErrorMessage(String existingErrorMessage, String nextErrorMessage) {
		if (!StringUtils.hasText(nextErrorMessage)) {
			return existingErrorMessage;
		}
		if (!StringUtils.hasText(existingErrorMessage)) {
			return nextErrorMessage;
		}
		return existingErrorMessage + System.lineSeparator() + nextErrorMessage;
	}

	@Override
	public FileTrackingDto getFileName(String correlationId) {
		Optional<FileTracking> fileTrackingOptional = this.fileTrackingRepository.findByCorrelationId(correlationId);
		if (fileTrackingOptional.isEmpty()) {
			throw new NotFoundException("No file found for correlation ID: " + correlationId);
		}
		FileTracking fileTracking = fileTrackingOptional.get();
		return fileTracking.toDto(new FileTrackingDto());
	}

}
