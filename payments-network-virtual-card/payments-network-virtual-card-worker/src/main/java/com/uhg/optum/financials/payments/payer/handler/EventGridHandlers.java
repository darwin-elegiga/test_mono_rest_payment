package com.uhg.optum.financials.payments.payer.handler;

import com.uhg.optum.financials.payments.payer.service.AuthFileService;
import com.uhg.optum.financials.payments.payer.service.ClearFileService;
import com.uhg.optum.financials.payments.payer.service.InvalidCardDeclineAuthFileService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.worker.dto.EventGridRequest;
import com.uhg.optum.financials.shared.worker.handlers.WorkerEventHandlers;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;

import java.util.regex.Pattern;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.AUTH_FILE_REGEX;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CLEAR_FILE_REGEX;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INVALID_CARD_DECLINE_FILE_REGEX;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.AUTH_ERROR;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.DocumentConstants.BLOBS_PATH;
import static com.uhg.optum.financials.payments.payer.constant.DocumentConstants.ERROR_MSG;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;

@Component
@Slf4j
public class EventGridHandlers extends WorkerEventHandlers.WorkerEventHandler<EventGridRequest> {

	@Autowired
	private AuthFileService authFileService;

	@Autowired
	private ClearFileService clearFileService;

	@Autowired
	private LogUtility logUtility;

	@Autowired
	private InvalidCardDeclineAuthFileService invalidCardDeclineAuthFileService;

	@Override
	public void handle(EventGridRequest eventGridRequest) {
		String filePath = getFilePath(eventGridRequest);
		String fileName = filePath.substring(filePath.lastIndexOf("/") + 1);

		if (isValidAuthFile(fileName)) {
			this.logUtility.logMessage("Received AUTH file with name: " + fileName, INFO, log);
			this.handleAuthFile(eventGridRequest);
		}
		else if (isValidClearFile(fileName)) {
			this.logUtility.logMessage("Received CLEAR file with name: " + fileName, INFO, log);
			this.handleClearFile(eventGridRequest);
		}
		else if (isValidInvalidCardDeclineAuthFile(fileName)) {
			this.logUtility.logMessage("Received Invalid Card Extract Auth file with name: " + fileName, INFO, log);
			this.handleInvalidCardDeclineAuthFile(eventGridRequest);
		}
		else {
			this.logUtility.logMessageWithPriority("Received Invalid File with name: " + fileName, ERROR, log, P3);
		}
	}

	private void handleAuthFile(EventGridRequest eventGridRequest) {
		try {
			this.authFileService.processAuthFile(getFilePath(eventGridRequest));
		}
		catch (Exception ex) {
			this.logUtility.logMessageWithPriority(
					AUTH_ERROR + getFilePath(eventGridRequest) + ERROR_MSG + ex.getMessage(), ERROR, log, P3);
		}
	}

	private void handleClearFile(EventGridRequest eventGridRequest) {
		try {
			this.clearFileService.processClearFile(getFilePath(eventGridRequest));
		}
		catch (Exception ex) {
			this.logUtility.logMessageWithPriority(
					"Error processing CLEAR file: " + getFilePath(eventGridRequest) + ERROR_MSG + ex.getMessage(),
					ERROR, log, P3);
		}
	}

	private void handleInvalidCardDeclineAuthFile(EventGridRequest eventGridRequest) {
		try {
			this.invalidCardDeclineAuthFileService.processInvalidAuthDeclineFile(getFilePath(eventGridRequest));
		}
		catch (Exception ex) {
			this.logUtility.logMessageWithPriority("Error processing Invalid Card Decline Auth file: "
					+ getFilePath(eventGridRequest) + ERROR_MSG + ex.getMessage(), ERROR, log, P3);
		}
	}

	private static String getFilePath(EventGridRequest eventGridRequest) {
		return eventGridRequest.getSubject()
			.substring(eventGridRequest.getSubject().lastIndexOf(BLOBS_PATH) + BLOBS_PATH.length());
	}

	private boolean isValidAuthFile(String fileName) {
		return Pattern.compile(AUTH_FILE_REGEX).matcher(fileName).matches();
	}

	private boolean isValidClearFile(String fileName) {
		return Pattern.compile(CLEAR_FILE_REGEX).matcher(fileName).matches();
	}

	private boolean isValidInvalidCardDeclineAuthFile(String fileName) {
		return Pattern.compile(INVALID_CARD_DECLINE_FILE_REGEX).matcher(fileName).matches();
	}

}
