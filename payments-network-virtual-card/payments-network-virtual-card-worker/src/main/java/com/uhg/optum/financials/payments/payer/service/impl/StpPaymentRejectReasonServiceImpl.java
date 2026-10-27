package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.stp.StpRejectReasonDto;
import com.uhg.optum.financials.payments.payer.enums.StpPaymentProcessingStatus;
import com.uhg.optum.financials.payments.payer.exception.StpPaymentRejectProcessingException;
import com.uhg.optum.financials.payments.payer.model.StpPaymentProcessingHistory;
import com.uhg.optum.financials.payments.payer.repository.StpPaymentProcessingHistoryRepository;
import com.uhg.optum.financials.payments.payer.repository.StpPaymentProcessingRepository;
import com.uhg.optum.financials.payments.payer.service.StpPaymentRejectReasonService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;

import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;

@Slf4j
@Service
public class StpPaymentRejectReasonServiceImpl implements StpPaymentRejectReasonService {

	private final LogUtility logUtility;

	private final StpPaymentProcessingHistoryRepository stpPaymentProcessingHistoryRepository;

	private final StpPaymentProcessingRepository stpPaymentProcessingRepository;

	public StpPaymentRejectReasonServiceImpl(LogUtility logUtility,
			StpPaymentProcessingHistoryRepository stpPaymentProcessingHistoryRepository,
			StpPaymentProcessingRepository stpPaymentProcessingRepository) {
		this.logUtility = logUtility;
		this.stpPaymentProcessingHistoryRepository = stpPaymentProcessingHistoryRepository;
		this.stpPaymentProcessingRepository = stpPaymentProcessingRepository;
	}

	@Override
	public void processRejectReason(StpRejectReasonDto dto) {
		log.info("Processing STP payment rejection for optumTransactionId={}", dto.getOptumTransactionId());
		try {
			updateStpPaymentProcessing(dto);
			savePaymentProcessingHistory(dto);
		}
		catch (Exception ex) {
			this.logUtility.logMessageWithPriority("Error while updating STP payment rejection for optumTransactionId: "
					+ dto.getOptumTransactionId() + " error: " + ex.getMessage(), ERROR, log, P3);
			throw new StpPaymentRejectProcessingException(ex.getMessage());
		}
		log.info("Completed STP payment rejection for optumTransactionId={}", dto.getOptumTransactionId());
	}

	private void savePaymentProcessingHistory(StpRejectReasonDto dto) {
		StpPaymentProcessingHistory historyEntity = new StpPaymentProcessingHistory();
		historyEntity.setNetworkPaymentId(dto.getOptumTransactionId());
		historyEntity.setStatus(StpPaymentProcessingStatus.STP_REJECTED);
		historyEntity.setCreatedAt(LocalDateTime.now());
		historyEntity.setRemarks(buildRemarks(dto));

		this.stpPaymentProcessingHistoryRepository.save(historyEntity);
	}

	private String buildRemarks(StpRejectReasonDto dto) {
		return "%s:%s".formatted(dto.getRejectionReasonCode(), dto.getRejectionReason());
	}

	private void updateStpPaymentProcessing(StpRejectReasonDto dto) {
		this.stpPaymentProcessingRepository.updateStatusToStpRejectedByNetworkPaymentId(dto.getOptumTransactionId(),
				StpPaymentProcessingStatus.STP_REJECTED, dto.getRejectionReasonCode(), dto.getRejectionReason());
	}

}
