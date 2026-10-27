package com.uhg.optum.financials.payments.payer.handler;

import com.uhg.optum.financials.payments.payer.dto.CancelCardDto;
import com.uhg.optum.financials.payments.payer.dto.CancelCardRequest;
import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import com.uhg.optum.financials.payments.payer.dto.SetECCKeysDto;
import com.uhg.optum.financials.payments.payer.dto.callback.CallbackError;
import com.uhg.optum.financials.payments.payer.dto.callback.CancelCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.CreateCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpRejectReasonDto;
import com.uhg.optum.financials.payments.payer.service.PaymentRequestService;
import com.uhg.optum.financials.payments.payer.service.SetECCKeysService;
import com.uhg.optum.financials.payments.payer.service.StpPaymentRejectReasonService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardCancelService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardResetService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardWebhookEventService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.worker.event.WorkerMessage;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.Instant;

import static com.uhg.optum.financials.payments.payer.constant.EventTypeConstants.AUTH_EVENT;
import static com.uhg.optum.financials.payments.payer.constant.EventTypeConstants.VIRTUAL_CARD_CANCEL;
import static com.uhg.optum.financials.payments.payer.constant.EventTypeConstants.VIRTUAL_CARD_CREATE;
import static com.uhg.optum.financials.payments.payer.constant.EventTypeConstants.VIRTUAL_CARD_RESET;
import static org.assertj.core.api.AssertionsForClassTypes.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;

@ExtendWith(MockitoExtension.class)
public class WorkerHandlerTests {

	@InjectMocks
	private WorkerHandler workerHandler;

	@Mock
	PaymentRequestService paymentRequestService;

	@Mock
	VirtualCardCancelService virtualCardCancelService;

	@Mock
	LogUtility logUtility;

	@Mock
	VirtualCardWebhookEventService virtualCardWebhookEventService;

	@Mock
	VirtualCardResetService virtualCardResetService;

	@Mock
	SetECCKeysService setECCKeysService;

	@Mock
	VirtualCardUtil virtualCardUtil;

	@Mock
	StpPaymentRejectReasonService stpPaymentRejectReasonService;

	private void allowWebhookProcessing() {
		BDDMockito.given(this.virtualCardUtil.tryMarkWebhookProcessed(anyString(), anyString())).willReturn(true);
	}

	@Test
	public void testHandleE1() {
		WorkerMessage workerMessage = new WorkerMessage();
		workerMessage.setPayload(new PaymentRequestDto());
		this.workerHandler.handle(workerMessage);
	}

	@Test
	public void testHandleE2() {
		WorkerMessage workerMessage = new WorkerMessage();
		workerMessage.setPayload(new CancelCardRequest());
		this.workerHandler.handle(workerMessage);
	}

	@Test
	public void testHandleE3Create() {
		allowWebhookProcessing();
		WorkerMessage workerMessage = new WorkerMessage();
		CreateCardData createCardData = new CreateCardData();
		Instant currentTime = Instant.now();
		ZetaWebhookDto zetaWebhookDto = new ZetaWebhookDto("1", "1.0", currentTime, "SUCCESS", VIRTUAL_CARD_CREATE,
				createCardData, null);
		workerMessage.setPayload(zetaWebhookDto);
		this.workerHandler.handle(workerMessage);
		BDDMockito.then(this.virtualCardWebhookEventService).should().handleCreate(any(CreateCardData.class));
	}

	@Test
	void handleE3_skipsDownstreamProcessing_whenWebhookWasAlreadyProcessed() {
		WorkerMessage<ZetaWebhookDto<CreateCardData>> workerMessage = new WorkerMessage<>();
		CreateCardData createCardData = new CreateCardData();
		ZetaWebhookDto<CreateCardData> zetaWebhookDto = new ZetaWebhookDto<>("1", "1.0", Instant.now(), "SUCCESS",
				VIRTUAL_CARD_CREATE, createCardData, null);
		workerMessage.setPayload(zetaWebhookDto);
		BDDMockito.given(this.virtualCardUtil.tryMarkWebhookProcessed(zetaWebhookDto.id(), zetaWebhookDto.type()))
			.willReturn(false);

		this.workerHandler.handle(workerMessage);

		BDDMockito.then(this.virtualCardWebhookEventService).shouldHaveNoInteractions();
	}

	@Test
	public void testHandleE3Cancel() {
		allowWebhookProcessing();
		WorkerMessage workerMessage = new WorkerMessage();
		CancelCardData cancelCardData = new CancelCardData();
		Instant currentTime = Instant.now();
		ZetaWebhookDto zetaWebhookDto = new ZetaWebhookDto("1", "1.0", currentTime, "SUCCESS", VIRTUAL_CARD_CANCEL,
				cancelCardData, null);
		workerMessage.setPayload(zetaWebhookDto);
		this.workerHandler.handle(workerMessage);
	}

	@Test
	public void testHandleE3Reset() {
		allowWebhookProcessing();
		WorkerMessage workerMessage = new WorkerMessage();
		Instant currentTime = Instant.now();
		ZetaWebhookDto zetaWebhookDto = new ZetaWebhookDto("1", "1.0", currentTime, "SUCCESS", VIRTUAL_CARD_RESET, null,
				null);

		workerMessage.setPayload(zetaWebhookDto);
		this.workerHandler.handle(workerMessage);
	}

	@Test
	public void testHandleInvalid() {
		WorkerMessage workerMessage = new WorkerMessage();
		workerMessage.setPayload(new Object());
		this.workerHandler.handle(workerMessage);
	}

	@Test
	public void testHandleInvalidType() {
		allowWebhookProcessing();
		WorkerMessage workerMessage = new WorkerMessage();
		Instant currentTime = Instant.now();
		ZetaWebhookDto zetaWebhookDto = new ZetaWebhookDto("1", "1.0", currentTime, "SUCCESS", "INVALID_TYPE", null,
				null);

		workerMessage.setPayload(zetaWebhookDto);
		assertThatThrownBy(() -> this.workerHandler.handle(workerMessage)).isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("INVALID_TYPE");

	}

	@Test
	public void testHandleE3CreateError() {
		allowWebhookProcessing();
		WorkerMessage workerMessage = new WorkerMessage();
		CallbackError callbackError = new CallbackError();
		callbackError.setCode("ERR001");
		callbackError.setMessage("Error message");
		Instant currentTime = Instant.now();
		ZetaWebhookDto zetaWebhookDto = new ZetaWebhookDto("1", "1.0", currentTime, "FAILED", VIRTUAL_CARD_CREATE, null,
				callbackError);

		workerMessage.setPayload(zetaWebhookDto);
		this.workerHandler.handle(workerMessage);
	}

	@Test
	public void testHandleE3CancelError() {
		allowWebhookProcessing();
		WorkerMessage workerMessage = new WorkerMessage();
		CallbackError callbackError = new CallbackError();
		callbackError.setCode("ERR001");
		callbackError.setMessage("Error message");
		Instant currentTime = Instant.now();
		ZetaWebhookDto zetaWebhookDto = new ZetaWebhookDto("1", "1.0", currentTime, "FAILED", VIRTUAL_CARD_CANCEL, null,
				callbackError);

		workerMessage.setPayload(zetaWebhookDto);
		this.workerHandler.handle(workerMessage);
	}

	@Test
	public void testHandleE4() {
		WorkerMessage workerMessage = new WorkerMessage();
		workerMessage.setPayload(new ResetCardRequest());
		this.workerHandler.handle(workerMessage);
	}

	@Test
	public void testHandleE4ResetError() {
		allowWebhookProcessing();
		WorkerMessage workerMessage = new WorkerMessage();
		Instant currentTime = Instant.now();
		CallbackError callbackError = new CallbackError();
		callbackError.setCode("ERR001");
		callbackError.setMessage("Error message");
		ZetaWebhookDto zetaWebhookDto = new ZetaWebhookDto("1", "2", currentTime, "FAILED", VIRTUAL_CARD_RESET, null,
				callbackError);
		workerMessage.setPayload(zetaWebhookDto);
		this.workerHandler.handle(workerMessage);
	}

	@Test
	public void cancelCard() {
		WorkerMessage workerMessage = new WorkerMessage();
		workerMessage.setPayload(new CancelCardDto());
		this.workerHandler.handle(workerMessage);
	}

	@Test
	public void testHandleE3Auth() {
		allowWebhookProcessing();
		WorkerMessage workerMessage = new WorkerMessage();
		Instant currentTime = Instant.now();
		ZetaWebhookDto zetaWebhookDto = new ZetaWebhookDto("1", "1.0", currentTime, "SUCCESS", AUTH_EVENT, null, null);

		workerMessage.setPayload(zetaWebhookDto);
		this.workerHandler.handle(workerMessage);
	}

	@Test
	public void testHandleE3AuthException() throws Exception {
		allowWebhookProcessing();
		WorkerMessage workerMessage = new WorkerMessage();
		Instant currentTime = Instant.now();
		ZetaWebhookDto zetaWebhookDto = new ZetaWebhookDto("1", "1.0", currentTime, "SUCCESS", AUTH_EVENT, null, null);
		workerMessage.setPayload(zetaWebhookDto);

		// Simulate exception in handleAuthEvents
		BDDMockito.doThrow(new RuntimeException("Simulated error"))
			.when(this.virtualCardWebhookEventService)
			.handleAuthEvents(any());

		Assertions.assertThatThrownBy(() -> this.workerHandler.handle(workerMessage))
			.isInstanceOf(RuntimeException.class)
			.hasMessageContaining("Error processing auth event callback: Simulated error");
	}

	@Test
	void handle_withSetECCKeysDto_callsHandleECCKey() {
		WorkerMessage<SetECCKeysDto> workerMessage = new WorkerMessage<>();
		workerMessage.setPayload(new SetECCKeysDto());
		this.workerHandler.handle(workerMessage);
		Mockito.verify(this.setECCKeysService, Mockito.times(1)).setKeys(Mockito.any(SetECCKeysDto.class));
	}

	@Test
	void handle_withStpRejectReason_callsRejectProcessor() {
		WorkerMessage<StpRejectReasonDto> workerMessage = new WorkerMessage<>();
		workerMessage.setPayload(new StpRejectReasonDto());
		this.workerHandler.handle(workerMessage);
		Mockito.verify(this.stpPaymentRejectReasonService, Mockito.times(1))
			.processRejectReason(Mockito.any(StpRejectReasonDto.class));
	}

}
