package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.repository.FileProcessingStatusRepository;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CLEAR;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;

@Service
@Slf4j
public class ClearExtractFileAlertService {

	@Autowired
	private FileProcessingStatusRepository fileProcessingStatusRepository;

	@Autowired
	private LogUtility logUtility;

	@Scheduled(cron = "${alert.cron-expression}")
	public void checkClearExtractFileArrival() {
		raiseAlert();
	}

	private void raiseAlert() {

		LocalDateTime last24Hours = LocalDateTime.now().minusHours(24);
		int count = this.fileProcessingStatusRepository.countRecordsCreatedOn(last24Hours, CLEAR);
		if (count == 0) {
			this.logUtility.logMessageWithPriority("No clearance files received on: " + last24Hours, ERROR, log, "P3");
		}
	}

}
