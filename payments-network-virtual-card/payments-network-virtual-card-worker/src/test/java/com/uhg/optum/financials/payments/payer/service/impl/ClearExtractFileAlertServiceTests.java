package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.repository.FileProcessingStatusRepository;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
public class ClearExtractFileAlertServiceTests {

	@InjectMocks
	ClearExtractFileAlertService clearExtractFileAlertService;

	@Mock
	LogUtility logUtility;

	@Mock
	FileProcessingStatusRepository fileProcessingStatusRepository;

	@Test
	public void testClearAlerts() {
		this.clearExtractFileAlertService.checkClearExtractFileArrival();
		BDDMockito.verify(this.fileProcessingStatusRepository).countRecordsCreatedOn(any(), any());
	}

	@Test
	public void testClearAlertsWithCount() {
		BDDMockito.when(this.fileProcessingStatusRepository.countRecordsCreatedOn(any(), any())).thenReturn(1);
		this.clearExtractFileAlertService.checkClearExtractFileArrival();
		BDDMockito.verify(this.fileProcessingStatusRepository).countRecordsCreatedOn(any(), any());
	}

}
