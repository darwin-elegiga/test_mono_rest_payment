package com.uhg.optum.financials.payments.payer.handler;

import com.uhg.optum.financials.payments.payer.service.AuthFileService;
import com.uhg.optum.financials.payments.payer.service.ClearFileService;
import com.uhg.optum.financials.payments.payer.service.InvalidCardDeclineAuthFileService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.worker.dto.EventGridRequest;
import org.assertj.core.api.BDDAssertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.Mockito;
import org.springframework.util.ReflectionUtils;

class EventGridHandlersTests {

	private AuthFileService authFileService;

	private ClearFileService clearFileService;

	private LogUtility logUtility;

	private InvalidCardDeclineAuthFileService invalidCardDeclineAuthFileService;

	private EventGridHandlers eventGridHandlers;

	@BeforeEach
	void setUp() {
		this.authFileService = Mockito.mock(AuthFileService.class);
		this.clearFileService = Mockito.mock(ClearFileService.class);
		this.logUtility = Mockito.mock(LogUtility.class);
		this.invalidCardDeclineAuthFileService = Mockito.mock(InvalidCardDeclineAuthFileService.class);
		this.eventGridHandlers = new EventGridHandlers();
		// Use reflection to inject mocks
		injectField(this.eventGridHandlers, "authFileService", this.authFileService);
		injectField(this.eventGridHandlers, "clearFileService", this.clearFileService);
		injectField(this.eventGridHandlers, "logUtility", this.logUtility);
		injectField(this.eventGridHandlers, "invalidCardDeclineAuthFileService",
				this.invalidCardDeclineAuthFileService);
	}

	private void injectField(Object target, String fieldName, Object value) {
		try {
			java.lang.reflect.Field field = target.getClass().getDeclaredField(fieldName);
			field.setAccessible(true);
			field.set(target, value);
		}
		catch (Exception exception) {
			throw new RuntimeException(exception);
		}
	}

	@Test
	void testHandle_validAuthFile_callsAuthFileService() {
		EventGridRequest eventGridRequest = Mockito.mock(EventGridRequest.class);
		Mockito.when(eventGridRequest.getSubject()).thenReturn("/blobs/path/authfile.txt");
		injectStaticRegex("AUTH_FILE_REGEX", ".*authfile\\.txt$");
		injectStaticRegex("CLEAR_FILE_REGEX", ".*clearfile\\.txt$");
		injectStaticRegex("INVALID_CARD_DECLINE_FILE_REGEX", ".*invalidfile\\.txt$");
		injectStaticBlobsPath("BLOBS_PATH", "/blobs/path/");
		Mockito.doNothing().when(this.authFileService).processAuthFile(Mockito.anyString());
		Mockito.doNothing().when(this.logUtility).logMessage(Mockito.anyString(), Mockito.anyString(), Mockito.any());
		this.eventGridHandlers.handle(eventGridRequest);
		Mockito.verify(this.authFileService).processAuthFile("authfile.txt");
		Mockito.verify(this.logUtility).logMessage(Mockito.contains("AUTH file"), Mockito.anyString(), Mockito.any());
	}

	@Test
	void testHandle_validClearFile_callsClearFileService() {
		EventGridRequest eventGridRequest = Mockito.mock(EventGridRequest.class);
		Mockito.when(eventGridRequest.getSubject()).thenReturn("/blobs/path/clearfile.txt");
		injectStaticRegex("AUTH_FILE_REGEX", ".*authfile\\.txt$");
		injectStaticRegex("CLEAR_FILE_REGEX", ".*clearfile\\.txt$");
		injectStaticRegex("INVALID_CARD_DECLINE_FILE_REGEX", ".*invalidfile\\.txt$");
		injectStaticBlobsPath("BLOBS_PATH", "/blobs/path/");
		Mockito.doNothing().when(this.clearFileService).processClearFile(Mockito.anyString());
		Mockito.doNothing().when(this.logUtility).logMessage(Mockito.anyString(), Mockito.anyString(), Mockito.any());
		this.eventGridHandlers.handle(eventGridRequest);
		Mockito.verify(this.clearFileService).processClearFile("clearfile.txt");
		Mockito.verify(this.logUtility).logMessage(Mockito.contains("CLEAR file"), Mockito.anyString(), Mockito.any());
	}

	@Test
	void testHandle_validInvalidCardDeclineAuthFile_callsInvalidCardDeclineAuthFileService() {
		EventGridRequest eventGridRequest = Mockito.mock(EventGridRequest.class);
		Mockito.when(eventGridRequest.getSubject()).thenReturn("/blobs/path/invalidfile.txt");
		injectStaticRegex("AUTH_FILE_REGEX", ".*authfile\\.txt$");
		injectStaticRegex("CLEAR_FILE_REGEX", ".*clearfile\\.txt$");
		injectStaticRegex("INVALID_CARD_DECLINE_FILE_REGEX", ".*invalidfile\\.txt$");
		injectStaticBlobsPath("BLOBS_PATH", "/blobs/path/");
		Mockito.doNothing()
			.when(this.invalidCardDeclineAuthFileService)
			.processInvalidAuthDeclineFile(Mockito.anyString());
		Mockito.doNothing().when(this.logUtility).logMessage(Mockito.anyString(), Mockito.anyString(), Mockito.any());
		this.eventGridHandlers.handle(eventGridRequest);
		Mockito.verify(this.invalidCardDeclineAuthFileService).processInvalidAuthDeclineFile("invalidfile.txt");
		Mockito.verify(this.logUtility)
			.logMessage(Mockito.contains("Invalid Card Extract Auth file"), Mockito.anyString(), Mockito.any());
	}

	@Test
	void testHandle_invalidFile_logsError() {
		EventGridRequest eventGridRequest = Mockito.mock(EventGridRequest.class);
		Mockito.when(eventGridRequest.getSubject()).thenReturn("/blobs/path/otherfile.txt");
		injectStaticRegex("AUTH_FILE_REGEX", ".*authfile\\.txt$");
		injectStaticRegex("CLEAR_FILE_REGEX", ".*clearfile\\.txt$");
		injectStaticRegex("INVALID_CARD_DECLINE_FILE_REGEX", ".*invalidfile\\.txt$");
		injectStaticBlobsPath("BLOBS_PATH", "/blobs/path/");
		Mockito.doNothing()
			.when(this.logUtility)
			.logMessageWithPriority(Mockito.anyString(), Mockito.anyString(), Mockito.any(), Mockito.any());
		this.eventGridHandlers.handle(eventGridRequest);
		Mockito.verify(this.logUtility)
			.logMessageWithPriority(Mockito.contains("Invalid File"), Mockito.anyString(), Mockito.any(),
					Mockito.any());
	}

	@Test
	void testHandleAuthFile_handlesException() {
		EventGridRequest eventGridRequest = Mockito.mock(EventGridRequest.class);
		Mockito.when(eventGridRequest.getSubject()).thenReturn("/blobs/path/authfile.txt");
		injectStaticRegex("AUTH_FILE_REGEX", ".*authfile\\.txt$");
		injectStaticBlobsPath("BLOBS_PATH", "/blobs/path/");
		Mockito.doThrow(new RuntimeException("fail")).when(this.authFileService).processAuthFile(Mockito.anyString());
		Mockito.doNothing()
			.when(this.logUtility)
			.logMessageWithPriority(Mockito.anyString(), Mockito.anyString(), Mockito.any(), Mockito.any());
		this.eventGridHandlers.handle(eventGridRequest);
		Mockito.verify(this.logUtility)
			.logMessageWithPriority(Mockito.contains("fail"), Mockito.anyString(), Mockito.any(), Mockito.any());
	}

	@Test
	void testHandleClearFile_handlesException() {
		EventGridRequest eventGridRequest = Mockito.mock(EventGridRequest.class);
		Mockito.when(eventGridRequest.getSubject()).thenReturn("/blobs/path/clearfile.txt");
		injectStaticRegex("CLEAR_FILE_REGEX", ".*clearfile\\.txt$");
		injectStaticBlobsPath("BLOBS_PATH", "/blobs/path/");
		Mockito.doThrow(new RuntimeException("fail")).when(this.clearFileService).processClearFile(Mockito.anyString());
		Mockito.doNothing()
			.when(this.logUtility)
			.logMessageWithPriority(Mockito.anyString(), Mockito.anyString(), Mockito.any(), Mockito.any());
		this.eventGridHandlers.handle(eventGridRequest);
		Mockito.verify(this.logUtility)
			.logMessageWithPriority(Mockito.contains("fail"), Mockito.anyString(), Mockito.any(), Mockito.any());
	}

	@Test
	void testHandleInvalidCardDeclineAuthFile_handlesException() {
		EventGridRequest eventGridRequest = Mockito.mock(EventGridRequest.class);
		Mockito.when(eventGridRequest.getSubject()).thenReturn("/blobs/path/invalidfile.txt");
		injectStaticRegex("INVALID_CARD_DECLINE_FILE_REGEX", ".*invalidfile\\.txt$");
		injectStaticBlobsPath("BLOBS_PATH", "/blobs/path/");
		Mockito.doThrow(new RuntimeException("fail"))
			.when(this.invalidCardDeclineAuthFileService)
			.processInvalidAuthDeclineFile(Mockito.anyString());
		Mockito.doNothing()
			.when(this.logUtility)
			.logMessageWithPriority(Mockito.anyString(), Mockito.anyString(), Mockito.any(), Mockito.any());
		this.eventGridHandlers.handle(eventGridRequest);
		Mockito.verify(this.logUtility)
			.logMessageWithPriority(Mockito.contains("fail"), Mockito.anyString(), Mockito.any(), Mockito.any());
	}

	@Test
	void testIsValidAuthFile() {
		String fileName = "authfile.txt";
		injectStaticRegex("AUTH_FILE_REGEX", ".*authfile\\.txt$");
		boolean result = invokeIsValidAuthFile(fileName);
		BDDAssertions.then(result).isTrue();
	}

	@Test
	void testIsValidClearFile() {
		String fileName = "clearfile.txt";
		injectStaticRegex("CLEAR_FILE_REGEX", ".*clearfile\\.txt$");
		boolean result = invokeIsValidClearFile(fileName);
		BDDAssertions.then(result).isTrue();
	}

	@Test
	void testIsValidInvalidCardDeclineAuthFile() {
		String fileName = "invalidfile.txt";
		injectStaticRegex("INVALID_CARD_DECLINE_FILE_REGEX", ".*invalidfile\\.txt$");
		boolean result = invokeIsValidInvalidCardDeclineAuthFile(fileName);
		BDDAssertions.then(result).isTrue();
	}

	@Test
	void testGetFilePath() {
		EventGridRequest eventGridRequest = Mockito.mock(EventGridRequest.class);
		injectStaticBlobsPath("BLOBS_PATH", "/blobs/path/");
		Mockito.when(eventGridRequest.getSubject()).thenReturn("/blobs/path/authfile.txt");
		String result = invokeGetFilePath(eventGridRequest);
		BDDAssertions.then(result).isEqualTo("authfile.txt");
	}

	// Reflection helpers for static fields and private methods
	private void injectStaticRegex(String fieldName, String value) {
		try {
			java.lang.reflect.Field field = Class
				.forName("com.uhg.optum.financials.payments.payer.constant.AppConstants")
				.getDeclaredField(fieldName);
			ReflectionUtils.makeAccessible(field);
			ReflectionUtils.setField(field, null, value);
		}
		catch (Exception exception) {
			throw new RuntimeException(exception);
		}
	}

	private void injectStaticBlobsPath(String fieldName, String value) {
		try {
			java.lang.reflect.Field field = Class
				.forName("com.uhg.optum.financials.payments.payer.constant.DocumentConstants")
				.getDeclaredField(fieldName);
			ReflectionUtils.makeAccessible(field);
			ReflectionUtils.setField(field, null, value);
		}
		catch (Exception exception) {
			throw new RuntimeException(exception);
		}
	}

	private boolean invokeIsValidAuthFile(String fileName) {
		try {
			java.lang.reflect.Method method = EventGridHandlers.class.getDeclaredMethod("isValidAuthFile",
					String.class);
			method.setAccessible(true);
			return (boolean) method.invoke(this.eventGridHandlers, fileName);
		}
		catch (Exception exception) {
			throw new RuntimeException(exception);
		}
	}

	private boolean invokeIsValidClearFile(String fileName) {
		try {
			java.lang.reflect.Method method = EventGridHandlers.class.getDeclaredMethod("isValidClearFile",
					String.class);
			method.setAccessible(true);
			return (boolean) method.invoke(this.eventGridHandlers, fileName);
		}
		catch (Exception exception) {
			throw new RuntimeException(exception);
		}
	}

	private boolean invokeIsValidInvalidCardDeclineAuthFile(String fileName) {
		try {
			java.lang.reflect.Method method = EventGridHandlers.class
				.getDeclaredMethod("isValidInvalidCardDeclineAuthFile", String.class);
			method.setAccessible(true);
			return (boolean) method.invoke(this.eventGridHandlers, fileName);
		}
		catch (Exception exception) {
			throw new RuntimeException(exception);
		}
	}

	private String invokeGetFilePath(EventGridRequest eventGridRequest) {
		try {
			java.lang.reflect.Method method = EventGridHandlers.class.getDeclaredMethod("getFilePath",
					EventGridRequest.class);
			method.setAccessible(true);
			return (String) method.invoke(null, eventGridRequest);
		}
		catch (Exception exception) {
			throw new RuntimeException(exception);
		}
	}

}
