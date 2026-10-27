package com.uhg.optum.financials.payments.payer.handler;

import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.Constants.FAILED_EVENT;
import static com.uhg.optum.financials.payments.payer.constant.Constants.SUCCESS_EVENT;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.constant.EventTypeConstants;
import com.uhg.optum.financials.payments.payer.dto.CancelCardDto;
import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import com.uhg.optum.financials.payments.payer.dto.SetECCKeysDto;
import com.uhg.optum.financials.payments.payer.dto.authevent.AuthorizationPayload;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.dto.callback.CancelCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.CreateCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.CallbackError;
import com.uhg.optum.financials.payments.payer.dto.callback.ResetCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpProviderEnrollmentRequestDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpRejectReasonDto;
import com.uhg.optum.financials.payments.payer.service.SetECCKeysService;
import com.uhg.optum.financials.payments.payer.service.StpEnrollmentService;
import com.uhg.optum.financials.payments.payer.service.StpPaymentRejectReasonService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardCancelService;
import com.uhg.optum.financials.payments.payer.service.PaymentRequestService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardDistributionService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardResetService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardWebhookEventService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.worker.event.WorkerMessage;
import com.uhg.optum.financials.shared.worker.handlers.WorkerEventHandlers;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;

@Component
@Slf4j
public class WorkerHandler extends
		WorkerEventHandlers.WorkerEventHandler3Events<WorkerMessage<?>, PaymentRequestDto, CancelCardDto, ZetaWebhookDto> {

	@Autowired
	PaymentRequestService paymentRequestService;

	@Autowired
	VirtualCardCancelService virtualCardCancelService;

	@Autowired
	LogUtility logUtility;

	@Autowired
	VirtualCardWebhookEventService virtualCardWebhookEventService;

	@Autowired
	VirtualCardResetService virtualCardResetService;

	@Autowired
	VirtualCardDistributionService virtualCardDistributionService;

	@Autowired
	SetECCKeysService setECCKeysService;

	@Autowired
	VirtualCardUtil virtualCardUtil;

	@Autowired
	StpEnrollmentService stpEnrollmentService;

	@Autowired
	StpPaymentRejectReasonService stpPaymentRejectReasonService;

	@Override
	public void handle(WorkerMessage<?> workerMessage) {
		if (workerMessage.getPayload() instanceof PaymentRequestDto) {
			handleE1((PaymentRequestDto) workerMessage.getPayload());
		}
		else if (workerMessage.getPayload() instanceof CancelCardDto) {
			handleE2((CancelCardDto) workerMessage.getPayload());
		}
		else if (workerMessage.getPayload() instanceof ZetaWebhookDto) {
			handleE3((ZetaWebhookDto) workerMessage.getPayload());
		}
		else if (workerMessage.getPayload() instanceof ResetCardRequest) {
			handleE4((ResetCardRequest) workerMessage.getPayload());
		}
		else if (workerMessage.getPayload() instanceof CorrelationDto) {
			handleE5((CorrelationDto) workerMessage.getPayload());
		}
		else if (workerMessage.getPayload() instanceof SetECCKeysDto) {
			handleECCKey((SetECCKeysDto) workerMessage.getPayload());
		}
		else if (workerMessage.getPayload() instanceof StpProviderEnrollmentRequestDto) {
			handleStpEnrollment((StpProviderEnrollmentRequestDto) workerMessage.getPayload());
		}
		else if (workerMessage.getPayload() instanceof StpRejectReasonDto) {
			handleStpPaymentReject((StpRejectReasonDto) workerMessage.getPayload());
		}
		else {
			log.error("Invalid message received :{}", workerMessage.getPayload());
		}
	}

	@Override
	public void handleE1(PaymentRequestDto event) {
		log.debug("Received message for payment processing :{}", event);
		this.paymentRequestService.processPaymentRequestEvents(event);
	}

	@Override
	public void handleE2(CancelCardDto event) {
		log.debug("Received message for cancel card :{}", event);
		this.virtualCardCancelService.processCancelCardRequest(event);
	}

	private void handleE4(ResetCardRequest payload) {
		log.debug("Received message for reset card :{}", payload);
		this.virtualCardResetService.processResetCardRequest(payload);
	}

	private void handleE5(CorrelationDto payload) {
		log.debug("Received message for correlation event processing :{}", payload);
		this.virtualCardDistributionService.processCorrelationBatchEvent(payload);
	}

	@Override
	public void handleE3(ZetaWebhookDto event) {
		log.info("Received message for Zeta Webhook :{}", event);
		ObjectMapper objectMapper = new ObjectMapper();
		if (!this.virtualCardUtil.tryMarkWebhookProcessed(event.id(), event.type())) {
			this.logUtility.logMessage(
					"Skipping already processed Zeta callback for id: " + event.id() + " and type: " + event.type(),
					ERROR, log);
			return;
		}
		switch (event.type()) {
			case EventTypeConstants.VIRTUAL_CARD_CREATE:
				if (FAILED_EVENT.equalsIgnoreCase(event.status())) {
					this.virtualCardWebhookEventService.handleCallbackError(
							objectMapper.convertValue(event.error(), CallbackError.class), event.type());

				}
				else if (SUCCESS_EVENT.equalsIgnoreCase(event.status())) {
					CreateCardData createCardData = objectMapper.convertValue(event.data(), CreateCardData.class);
					this.virtualCardWebhookEventService.handleCreate(createCardData);

				}
				break;
			case EventTypeConstants.VIRTUAL_CARD_CANCEL:
				if (FAILED_EVENT.equalsIgnoreCase(event.status())) {
					this.virtualCardWebhookEventService.handleCallbackError(
							objectMapper.convertValue(event.error(), CallbackError.class), event.type());

				}
				else if (SUCCESS_EVENT.equalsIgnoreCase(event.status())) {
					CancelCardData cancelCardData = objectMapper.convertValue(event.data(), CancelCardData.class);
					this.virtualCardWebhookEventService.handleCancel(cancelCardData);
				}
				break;
			case EventTypeConstants.VIRTUAL_CARD_RESET:
				if (FAILED_EVENT.equalsIgnoreCase(event.status())) {
					this.virtualCardWebhookEventService.handleCallbackError(
							objectMapper.convertValue(event.error(), CallbackError.class), event.type());

				}
				else if (SUCCESS_EVENT.equalsIgnoreCase(event.status())) {
					ResetCardData resetCardData = objectMapper.convertValue(event.data(), ResetCardData.class);
					this.virtualCardWebhookEventService.handleReset(resetCardData);
				}
				break;
			case EventTypeConstants.AUTH_EVENT:
				objectMapper.registerModule(new com.fasterxml.jackson.datatype.jsr310.JavaTimeModule());
				AuthorizationPayload authorizationPayload = objectMapper.convertValue(event.data(),
						AuthorizationPayload.class);
				try {
					this.virtualCardWebhookEventService.handleAuthEvents(authorizationPayload);
				}
				catch (Exception ex) {
					throw new RuntimeException("Error processing auth event callback: " + ex.getMessage(), ex);
				}
				break;
			default:
				this.logUtility.logMessage("Unsupported Zeta callback type: " + event.type(), ERROR, log);
				throw new IllegalArgumentException("Unsupported Zeta callback type: " + event.type());
		}
	}

	private void handleECCKey(SetECCKeysDto payload) {
		this.setECCKeysService.setKeys(payload);
	}

	private void handleStpEnrollment(StpProviderEnrollmentRequestDto payload) {
		log.debug("Received message for STP enrollment processing :{}", payload);
		this.stpEnrollmentService.processEnrollmentRequest(payload);
	}

	private void handleStpPaymentReject(StpRejectReasonDto payload) {
		log.debug("Received message for STP payment reject processing :{}", payload);
		this.stpPaymentRejectReasonService.processRejectReason(payload);
	}

}
