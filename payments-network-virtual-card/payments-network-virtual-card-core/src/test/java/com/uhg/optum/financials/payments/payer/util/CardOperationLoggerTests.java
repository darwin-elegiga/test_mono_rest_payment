package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.dto.ResponseObject;
import com.uhg.optum.financials.payments.payer.enums.CardEventType;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import org.slf4j.Logger;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.test.util.ReflectionTestUtils;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.times;

class CardOperationLoggerTests {

	@Mock
	private LogUtility thisLogUtility;

	@Mock
	private Logger thisLogger;

	private CardOperationLogger thisCardOperationLogger;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
		this.thisCardOperationLogger = new CardOperationLogger();
		// Inject mock LogUtility using Spring's ReflectionTestUtils
		ReflectionTestUtils.setField(this.thisCardOperationLogger, "logUtility", this.thisLogUtility);
	}

	@Test
	@DisplayName("logCardProcessStarted logs correct message")
	void logCardProcessStarted_shouldLogCorrectMessage() {
		// given
		CardEventType thisEventType = CardEventType.CREATE;
		String thisPaymentId = "paymentId";
		String thisRequestId = "requestId";
		Object thisPayload = "payload";
		String thisStatus = "STARTED";
		// when
		this.thisCardOperationLogger.logCardProcessStarted(thisEventType, thisPaymentId, thisRequestId, thisPayload,
				thisStatus, this.thisLogger);
		// then
		String thisExpectedMessage = String.format(
				"[paymentId=%s] [requestId=%s] [%s][%s] Card operation started. Payload: %s", thisPaymentId,
				thisRequestId, thisEventType.name(), thisStatus, thisPayload);
		verify(this.thisLogUtility, times(1)).logMessage(thisExpectedMessage, INFO, this.thisLogger);
	}

	@Test
	@DisplayName("logCardProcessCompleted logs correct message")
	void logCardProcessCompleted_shouldLogCorrectMessage() {
		// given
		CardEventType thisEventType = CardEventType.CANCEL;
		String thisPaymentId = "paymentId";
		String thisRequestId = "requestId";
		Object thisPayload = "payload";
		String thisStatus = "COMPLETED";
		// when
		this.thisCardOperationLogger.logCardProcessCompleted(thisEventType, thisPaymentId, thisRequestId, thisPayload,
				thisStatus, this.thisLogger);
		// then
		String thisExpectedMessage = String.format(
				"[paymentId=%s] [requestId=%s] [%s][%s] Card operation completed. Payload: %s", thisPaymentId,
				thisRequestId, thisEventType.name(), thisStatus, thisPayload);
		verify(this.thisLogUtility, times(1)).logMessage(thisExpectedMessage, INFO, this.thisLogger);
	}

	@Test
	@DisplayName("logCardProcessError logs correct message with response")
	void logCardProcessError_shouldLogCorrectMessage_withResponse() {
		// given
		CardEventType thisEventType = CardEventType.RESET;
		String thisPaymentId = "paymentId";
		String thisRequestId = "requestId";
		ResponseObject thisResponseObject = new ResponseObject();
		thisResponseObject.setMessage("error message");
		ResponseEntity<ResponseObject> thisResponse = new ResponseEntity<>(thisResponseObject, HttpStatus.BAD_REQUEST);
		// when
		this.thisCardOperationLogger.logCardProcessError(thisEventType, thisPaymentId, thisRequestId, thisResponse,
				this.thisLogger);
		// then
		String thisExpectedLogMessage = String.format(
				"[paymentId=%s] [requestId=%s] [%s][ERROR] Card operation error. Status: %s, Message: %s",
				thisPaymentId, thisRequestId, thisEventType.name(), thisResponse.getStatusCode().toString(),
				"error message");
		verify(this.thisLogUtility, times(1)).logMessage(thisExpectedLogMessage, ERROR, this.thisLogger);
	}

	@Test
	@DisplayName("logCardProcessError logs correct message with null response")
	void logCardProcessError_shouldLogCorrectMessage_withNullResponse() {
		// given
		CardEventType thisEventType = CardEventType.RESET;
		String thisPaymentId = "paymentId";
		String thisRequestId = "requestId";
		ResponseEntity<ResponseObject> thisResponse = null;
		// when
		this.thisCardOperationLogger.logCardProcessError(thisEventType, thisPaymentId, thisRequestId, thisResponse,
				this.thisLogger);
		// then
		String thisExpectedLogMessage = String.format(
				"[paymentId=%s] [requestId=%s] [%s][ERROR] Card operation error. Status: %s, Message: %s",
				thisPaymentId, thisRequestId, thisEventType.name(), "Unknown Status", "No message available");
		verify(this.thisLogUtility, times(1)).logMessage(thisExpectedLogMessage, ERROR, this.thisLogger);
	}

	@Test
	@DisplayName("logCardProcessException logs correct message")
	void logCardProcessException_shouldLogCorrectMessage() {
		// given
		CardEventType thisEventType = CardEventType.AUTH;
		String thisPaymentId = "paymentId";
		String thisRequestId = "requestId";
		Exception thisException = new Exception("exception message");
		// when
		this.thisCardOperationLogger.logCardProcessException(thisEventType, thisPaymentId, thisRequestId, thisException,
				this.thisLogger);
		// then
		String thisExpectedLogMessage = String.format(
				"[paymentId=%s] [requestId=%s] [%s][EXCEPTION] Card operation exception. Exception: %s", thisPaymentId,
				thisRequestId, thisEventType.name(), thisException.getMessage());
		verify(this.thisLogUtility, times(1)).logMessage(thisExpectedLogMessage, ERROR, this.thisLogger);
	}

	@Test
	@DisplayName("extractResponseMessage returns correct message")
	void extractResponseMessage_shouldReturnMessage_whenPresent() {
		// given
		ResponseObject thisResponseObject = new ResponseObject();
		thisResponseObject.setMessage("response message");
		ResponseEntity<ResponseObject> thisResponse = new ResponseEntity<>(thisResponseObject, HttpStatus.OK);
		// when
		String thisResult = this.thisCardOperationLogger.extractResponseMessage(thisResponse);
		// then
		assertThat(thisResult).isEqualTo("response message");
	}

	@Test
	@DisplayName("extractResponseMessage returns default when message is missing")
	void extractResponseMessage_shouldReturnDefault_whenMessageMissing() {
		// given
		ResponseEntity<ResponseObject> thisResponse = ResponseEntity.ok().build();
		// when
		String thisResult = this.thisCardOperationLogger.extractResponseMessage(thisResponse);
		// then
		assertThat(thisResult).isEqualTo("No message available");
	}

}
