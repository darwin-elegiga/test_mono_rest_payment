package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.handler.WorkerHandler;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.worker.event.WorkerMessage;
import com.uhg.optum.financials.shared.worker.service.WorkerConsumerService;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.lang3.exception.ExceptionUtils;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.context.annotation.Bean;
import org.springframework.stereotype.Component;

import java.util.function.Consumer;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;

@Component
@Slf4j
@Qualifier("storageQueueConsumer")
public class StorageQueueConsumer implements WorkerConsumerService {

	@Autowired
	private WorkerHandler storageQueueHandlers;

	@Autowired
	private LogUtility logUtility;

	@Bean(name = "workerMessageConsumer")
	@Override
	public Consumer<WorkerMessage<?>> getConsumer() {
		return (message) -> {
			try {
				this.storageQueueHandlers.handle(message);
			}
			catch (Exception ex) {
				this.logUtility.logMessage("Error occurred while processing message payload=" + message.getPayload()
						+ " error=" + ex + System.lineSeparator() + ExceptionUtils.getStackTrace(ex), ERROR, log);
				throw new InternalServerException(ex.getMessage());
			}
		};
	}

	@Bean(name = "workerErrorConsumer")
	@Override
	public Consumer<Throwable> getErrorConsumer() {
		return (throwable) -> this.logUtility.logMessage(
				"Error occurred " + throwable + System.lineSeparator() + ExceptionUtils.getStackTrace(throwable), ERROR,
				log);
	}

}
