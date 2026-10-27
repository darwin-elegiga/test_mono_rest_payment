package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.PaymentRequestTotalDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchCorrelationDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchDetailUpdateRequestDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchUpdateTimeRangeRequestDto;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelatedBatchDetail;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import org.springframework.http.ResponseEntity;

import java.util.List;

public interface BatchDetailService {

	void saveBatchDetail(CorrelationDto dto);

	ResponseEntity<Object> getBatchDetails(String correlationId);

	PaymentRequestTotalDto getPaymentCountAndAmountByStatus(String correlationId);

	List<CorrelatedBatchDetail> getCorrelatedBatch(String status);

	void updateBatchDetailStatus(String correlationId, String status, String oldStatus);

	long timeDifferenceInHours(String correlationId);

	List<BatchCorrelationDto> getBatchDetailsByFailStatus(String batchStatus, String enterDate);

	int updateBatchDetailsByStatus(BatchDetailUpdateRequestDto request);

	int updateBatchesByStatus(BatchUpdateTimeRangeRequestDto request);

	List<String> getCorrelationIdsList(int offset, int limit);

}
