package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.dto.ResponseObject;
import com.uhg.optum.financials.payments.payer.enums.CardEventType;
import org.slf4j.Logger;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Component;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;

@Component
public class CardOperationLogger {

	@Autowired
	private LogUtility logUtility;

	public void logCardProcessStarted(CardEventType eventType, String paymentId, String requestId, Object payload,
			String status, Logger log) {
		String message = String.format("[paymentId=%s] [requestId=%s] [%s][%s] Card operation started. Payload: %s",
				paymentId, requestId, eventType.name(), status, payload);
		this.logUtility.logMessage(message, INFO, log);
	}

	public void logCardProcessCompleted(CardEventType eventType, String paymentId, String requestId, Object payload,
			String status, Logger log) {
		String message = String.format("[paymentId=%s] [requestId=%s] [%s][%s] Card operation completed. Payload: %s",
				paymentId, requestId, eventType.name(), status, payload);
		this.logUtility.logMessage(message, INFO, log);
	}

	public void logCardProcessError(CardEventType eventType, String paymentId, String requestId,
			ResponseEntity<ResponseObject> response, Logger log) {
		String statusCode = (response != null && response.getStatusCode() != null) ? response.getStatusCode().toString()
				: "Unknown Status";

		String message = extractResponseMessage(response);

		String logMessage = String.format(
				"[paymentId=%s] [requestId=%s] [%s][ERROR] Card operation error. Status: %s, Message: %s", paymentId,
				requestId, eventType.name(), statusCode, message);

		this.logUtility.logMessage(logMessage, ERROR, log);
	}

	public void logCardProcessException(CardEventType eventType, String paymentId, String requestId, Exception ex,
			Logger log) {
		String logMessage = String.format(
				"[paymentId=%s] [requestId=%s] [%s][EXCEPTION] Card operation exception. Exception: %s", paymentId,
				requestId, eventType.name(), ex.getMessage());

		this.logUtility.logMessage(logMessage, ERROR, log);
	}

	public String extractResponseMessage(ResponseEntity<ResponseObject> response) {
		if (response != null && response.getBody() != null && response.getBody().getMessage() != null) {
			return response.getBody().getMessage();
		}
		return "No message available";
	}

}
