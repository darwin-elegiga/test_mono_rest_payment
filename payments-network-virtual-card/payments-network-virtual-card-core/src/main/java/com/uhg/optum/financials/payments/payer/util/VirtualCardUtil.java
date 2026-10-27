package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.constant.CommonConstants;
import com.uhg.optum.financials.payments.payer.constant.ValidationConstants;
import com.uhg.optum.financials.payments.payer.enums.Status;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.PaymentRequestHistory;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.model.VirtualCardHistory;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestHistoryRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.ProcessedWebhookRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardHistoryRepository;
import lombok.AllArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.modelmapper.ModelMapper;
import org.springframework.stereotype.Component;
import org.springframework.util.StringUtils;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;

@AllArgsConstructor
@Slf4j
@Component
public class VirtualCardUtil {

	private final LogUtility logUtility;

	private final CardCreationRepository cardCreationRepository;

	private final PaymentRequestRepository paymentRequestRepository;

	private final VirtualCardHistoryRepository virtualCardHistoryRepository;

	private final ProcessedWebhookRepository processedWebhookRepository;

	private final PaymentRequestHistoryRepository paymentRequestHistoryRepository;

	private final ModelMapper modelMapper;

	public CardCreation getCardCreationByRequestId(String requestId) {
		return this.cardCreationRepository.findById(UUID.fromString(requestId))
			.orElseThrow(() -> new IllegalArgumentException("CardCreation not found for requestId: " + requestId));
	}

	public PaymentRequest updatePaymentRequestStatus(String paymentId, Status status) {
		return updatePaymentRequestStatus(paymentId, status, null);
	}

	public PaymentRequest updatePaymentRequestStatus(String paymentId, Status status, String errorMessage) {
		Optional<PaymentRequest> optionalPaymentRequest = Optional
			.ofNullable(this.paymentRequestRepository.findByPaymentId(paymentId));
		optionalPaymentRequest.ifPresent((paymentRequest) -> {
			if (paymentRequest.getPaymentDetails() != null && status != null) {
				paymentRequest.setStatus(status.name());
				this.logUtility.logMessage("Payment request status set to: " + paymentRequest.getStatus(), INFO, log);
				paymentRequest.setUpdatedById(VIRTUAL_CARD);
				paymentRequest.setUpdatedOn(LocalDateTime.now());
				paymentRequest.setErrorMessage(appendErrorMessage(paymentRequest.getErrorMessage(), errorMessage));
				this.paymentRequestRepository.save(paymentRequest);
				savePaymentRequestHistory(paymentRequest);
			}
		});
		return optionalPaymentRequest
			.orElseThrow(() -> new IllegalArgumentException("PaymentRequest not found for requestId: " + paymentId));
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

	public void saveVirtualCardHistory(VirtualCard virtualCard, String reasonCode, String receiptId) {
		VirtualCardHistory virtualCardHistory = new VirtualCardHistory();
		virtualCardHistory.setReceiptId(receiptId);
		saveVirtualCardHistory(virtualCard, reasonCode, virtualCardHistory);
	}

	public void saveVirtualCardHistory(VirtualCard virtualCard, String reasonCode) {
		VirtualCardHistory virtualCardHistory = new VirtualCardHistory();
		saveVirtualCardHistory(virtualCard, reasonCode, virtualCardHistory);
	}

	private void saveVirtualCardHistory(VirtualCard virtualCard, String reasonCode,
			VirtualCardHistory virtualCardHistory) {
		virtualCardHistory.setCardGuid(virtualCard.getCardGuid());
		virtualCardHistory.setStatus(virtualCard.getStatus());
		virtualCardHistory.setPaymentId(virtualCard.getPaymentId());
		virtualCardHistory.setInitialBalance(virtualCard.getInitialBalance());
		virtualCardHistory.setAvailableBalance(virtualCard.getAvailableBalance());
		virtualCardHistory.setCurrentBalance(virtualCard.getCurrentBalance());
		virtualCardHistory.setCurrency(virtualCard.getCurrency());
		virtualCardHistory.setReason(reasonCode);
		virtualCardHistory.setCreatedBy(VIRTUAL_CARD);
		virtualCardHistory.setMaskedPan(virtualCard.getMaskedPan());
		virtualCardHistory.setCreatedAt(LocalDateTime.now());
		this.virtualCardHistoryRepository.save(virtualCardHistory);
	}

	public boolean tryMarkWebhookProcessed(String id, String eventType) {
		int inserted = this.processedWebhookRepository.tryInsertProcessed(id, eventType, AppConstants.PROCESSED);

		return switch (inserted) {
			case 0 -> {
				this.logUtility.logMessage("Duplicate processed webhook ignored for ID: %s".formatted(id),
						CommonConstants.INFO, log);
				yield false;
			}
			case 1 -> {
				this.logUtility.logMessage("Row inserted with ID: %s".formatted(id), CommonConstants.INFO, log);
				yield true;
			}
			default ->
				throw new IllegalStateException("Unexpected insert result [%d] for ID: %s".formatted(inserted, id));
		};
	}

	private void savePaymentRequestHistory(PaymentRequest paymentRequest) {
		// save payment request history
		PaymentRequestHistory paymentRequestHistory = new PaymentRequestHistory();

		paymentRequestHistory.setCorrelationId(paymentRequest.getCorrelationId());
		paymentRequestHistory.setIssuerPaymentId(paymentRequest.getPaymentDetails().getIssuerPaymentId());
		paymentRequestHistory.setReferenceId(paymentRequest.getPaymentDetails().getReferenceId());
		paymentRequestHistory.setRemitMethod(paymentRequest.getPaymentDetails().getRemitMethod());
		paymentRequestHistory.setAmount(paymentRequest.getPaymentDetails().getAmount());
		paymentRequestHistory.setStatus(paymentRequest.getStatus());
		paymentRequestHistory.setStSeSegment(paymentRequest.getStSeSegment());
		paymentRequestHistory.setSubmitterId(paymentRequest.getSubmitterId());
		paymentRequestHistory.setOrigin(paymentRequest.getOrigin());
		paymentRequestHistory.setPayr835id(paymentRequest.getPayr835Id());
		paymentRequestHistory.setOrgShortCode(paymentRequest.getOrgShortCode());
		paymentRequestHistory.setCreatedById(paymentRequest.getCreatedById());
		paymentRequestHistory.setAccountShortCode(paymentRequest.getAccountShortCode());
		paymentRequestHistory.setSubAccountShortCode(paymentRequest.getSubAccountShortCode());
		paymentRequestHistory.setTotalClmCount(paymentRequest.getTotalClmCount());
		paymentRequestHistory.setClmUrl(paymentRequest.getClmUrl());
		paymentRequestHistory.setDocType(paymentRequest.getDocType());
		paymentRequestHistory.setDocuments(paymentRequest.getDocuments());
		paymentRequestHistory.setMetadata(paymentRequest.getMetadata());
		paymentRequestHistory.setRePreferenceFlag(paymentRequest.isRePreferenceFlag());
		paymentRequestHistory.setCreatedOn(LocalDateTime.now());
		paymentRequestHistory.setPaymentDetailsId(paymentRequest.getPaymentDetails().getId());
		paymentRequestHistory.setErrorMessage(paymentRequest.getErrorMessage());
		this.paymentRequestHistoryRepository.save(paymentRequestHistory);
		this.logUtility.logMessage("Payment request history saved successfully", CommonConstants.INFO, log);
	}

	public void savePaymentRequestHistoryForIds(Set<String> paymentRequestIds) {
		List<PaymentRequest> paymentRequestList = this.paymentRequestRepository.findByIdIn(paymentRequestIds);
		this.logUtility.logMessage("Saving history records", ValidationConstants.INFO, log);
		List<PaymentRequestHistory> paymentRequestHistories = paymentRequestList.stream().map((paymentRequest) -> {
			PaymentRequestHistory history = new PaymentRequestHistory();

			// Map fields from PaymentRequest
			history.setCorrelationId(paymentRequest.getCorrelationId());
			history.setStatus(paymentRequest.getStatus());
			history.setStSeSegment(paymentRequest.getStSeSegment());
			history.setSubmitterId(paymentRequest.getSubmitterId());
			history.setOrigin(paymentRequest.getOrigin());
			history.setPayr835id(paymentRequest.getPayr835Id());
			history.setOrgShortCode(paymentRequest.getOrgShortCode());
			history.setCreatedById(paymentRequest.getCreatedById());
			history.setAccountShortCode(paymentRequest.getAccountShortCode());
			history.setSubAccountShortCode(paymentRequest.getSubAccountShortCode());
			history.setTotalClmCount(paymentRequest.getTotalClmCount());
			history.setClmUrl(paymentRequest.getClmUrl());
			history.setDocType(paymentRequest.getDocType());
			history.setPlb(paymentRequest.isPlb());
			history.setDocuments(paymentRequest.getDocuments());
			history.setMetadata(paymentRequest.getMetadata());
			history.setRePreferenceFlag(paymentRequest.isRePreferenceFlag());
			history.setErrorMessage(paymentRequest.getErrorMessage());

			// Map fields from PaymentDetails (nested object)
			if (paymentRequest.getPaymentDetails() != null) {
				history.setPaymentDetailsId(paymentRequest.getPaymentDetails().getId());
				history.setAmount(paymentRequest.getPaymentDetails().getAmount());
				history.setIssuerPaymentId(paymentRequest.getPaymentDetails().getIssuerPaymentId());
				history.setReferenceId(paymentRequest.getPaymentDetails().getReferenceId());
				history.setRemitMethod(paymentRequest.getPaymentDetails().getRemitMethod());
				history.setNetworkPaymentId(paymentRequest.getPaymentDetails().getNetworkPaymentId());
			}

			// Set audit fields
			history.setCreatedOn(LocalDateTime.now());
			// Explicitly set id to null to ensure a new record is inserted, not merged
			history.setId(null);

			return history;
		}).toList();

		this.paymentRequestHistoryRepository.saveAll(paymentRequestHistories);
	}

}
