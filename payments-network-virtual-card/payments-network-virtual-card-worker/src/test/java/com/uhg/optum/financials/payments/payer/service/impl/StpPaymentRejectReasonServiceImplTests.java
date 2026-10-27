package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.stp.StpRejectReasonDto;
import com.uhg.optum.financials.payments.payer.exception.StpPaymentRejectProcessingException;
import com.uhg.optum.financials.payments.payer.repository.StpPaymentProcessingHistoryRepository;
import com.uhg.optum.financials.payments.payer.repository.StpPaymentProcessingRepository;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InOrder;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import static com.uhg.optum.financials.payments.payer.enums.StpPaymentProcessingStatus.STP_REJECTED;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.inOrder;
import static org.mockito.Mockito.mock;

@ExtendWith(MockitoExtension.class)
class StpPaymentRejectReasonServiceImplTests {

	@InjectMocks
	private StpPaymentRejectReasonServiceImpl stpPaymentRejectReasonService;

	@Mock
	private StpPaymentProcessingHistoryRepository stpPaymentProcessingHistoryRepository;

	@Mock
	private StpPaymentProcessingRepository stpPaymentProcessingRepository;

	@Mock
	private LogUtility logUtility;

	@Test
	void processRejectReason_ShouldUpdateAndSaveHistory() {
		StpRejectReasonDto dto = mockDtoWithRejectDetails();

		this.stpPaymentRejectReasonService.processRejectReason(dto);

		InOrder inOrder = inOrder(this.stpPaymentProcessingRepository, this.stpPaymentProcessingHistoryRepository);
		inOrder.verify(this.stpPaymentProcessingRepository)
			.updateStatusToStpRejectedByNetworkPaymentId("txn-123", STP_REJECTED, "R001", "Invalid account");
		inOrder.verify(this.stpPaymentProcessingHistoryRepository).save(any());
	}

	@Test
	void processRejectReason_WhenRepositoryFails_ShouldThrowStpPaymentRejectProcessingException() {
		StpRejectReasonDto dto = mockDtoWithRejectDetails();
		given(this.stpPaymentProcessingHistoryRepository.save(any())).willThrow(new RuntimeException("db error"));

		assertThatThrownBy(() -> this.stpPaymentRejectReasonService.processRejectReason(dto))
			.isInstanceOf(StpPaymentRejectProcessingException.class)
			.hasMessage("db error");
	}

	private StpRejectReasonDto mockDtoWithRejectDetails() {
		StpRejectReasonDto dto = mock(StpRejectReasonDto.class);
		given(dto.getOptumTransactionId()).willReturn("txn-123");
		given(dto.getRejectionReasonCode()).willReturn("R001");
		given(dto.getRejectionReason()).willReturn("Invalid account");
		return dto;
	}

}
