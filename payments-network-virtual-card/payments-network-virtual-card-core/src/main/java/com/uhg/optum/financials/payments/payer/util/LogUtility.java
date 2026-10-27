package com.uhg.optum.financials.payments.payer.util;

import org.slf4j.Logger;
import org.slf4j.MDC;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_PRIORITY;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.NAME;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.STATUS;

@Component
public final class LogUtility {

	@Value("${spring.application.name}")
	private String appName;

	private String appVersion = System.getenv("APPLICATION_VERSION");

	public static final String LOG_MESSAGE = "logMessage";

	public void logMessage(String logMessage, String loggerType, Logger log) {
		commonLog(logMessage, loggerType, log, null);
	}

	public void logMessageWithPriority(String logMessage, String loggerType, Logger log, String errorPriority) {
		commonLog(logMessage, loggerType, log, errorPriority);
	}

	public void commonLog(String logMessage, String loggerType, Logger log, String errorPriority) {
		if (errorPriority != null) {
			MDC.put(ERROR_PRIORITY, errorPriority);
		}
		MDC.put(LOG_MESSAGE, logMessage);
		MDC.put(NAME, this.appName);
		try {
			MDC.put("version", this.appVersion);
		}
		catch (Exception ex) {
			log.error("Failed to get version file", ex);
		}
		switch (loggerType) {
			case "error":
				log.error(MDC.get(LOG_MESSAGE));
				break;
			case "warn":
				log.warn(MDC.get(LOG_MESSAGE));
				break;
			case "debug":
				MDC.put(STATUS, "200");
				log.debug(MDC.get(LOG_MESSAGE));
				break;
			default:
				MDC.put(STATUS, "200");
				log.info(MDC.get(LOG_MESSAGE));
				break;
		}
		MDC.clear();
	}

}
