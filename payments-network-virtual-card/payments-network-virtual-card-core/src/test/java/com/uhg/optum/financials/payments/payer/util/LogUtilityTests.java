package com.uhg.optum.financials.payments.payer.util;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.slf4j.Logger;

@ExtendWith(MockitoExtension.class)
public class LogUtilityTests {

	@InjectMocks
	private LogUtility logUtility;

	@Test
	void logMessageTest() {

		String logMessage = "logMessage";
		Logger log = Mockito.mock(Logger.class);

		this.logUtility.logMessage(logMessage, "error", log);
		this.logUtility.logMessage(logMessage, "warn", log);
		this.logUtility.logMessage(logMessage, "debug", log);
		this.logUtility.logMessage(logMessage, "invalid", log);

	}

	@Test
	void logMessagePriorityTest() {

		String logMessage = "logMessage";
		Logger log = Mockito.mock(Logger.class);

		this.logUtility.logMessageWithPriority(logMessage, "error", log, "P3");
		this.logUtility.logMessageWithPriority(logMessage, "warn", log, "P3");
		this.logUtility.logMessageWithPriority(logMessage, "debug", log, "P3");
		this.logUtility.logMessageWithPriority(logMessage, "invalid", log, "P3");

	}

}
