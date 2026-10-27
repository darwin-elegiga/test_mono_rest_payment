package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.authevent.AuthorizationPayload;
import com.uhg.optum.financials.payments.payer.dto.callback.CreateCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.CallbackError;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.service.ZetaWebhookService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import java.time.Instant;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.SUCCESS;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;

@ExtendWith(MockitoExtension.class)
public class WebhookControllerTests {

	@InjectMocks
	WebhookController webhookController;

	@Mock
	ZetaWebhookService zetaWebhookService;

	@Mock
	LogUtility logUtility;

	@Test
	public void testWebhookController() {
		CreateCardData createCardData = new CreateCardData();
		CallbackError error = new CallbackError();
		Instant currentTime = Instant.now();

		ZetaWebhookDto zetaWebhookDto = new ZetaWebhookDto("1", "1.0", currentTime, "status", "type", createCardData,
				error);
		given(this.zetaWebhookService.processZetaWebhook(any())).willReturn(1);
		Assertions.assertThat(this.webhookController.processZetaCallbackResponse(zetaWebhookDto).getStatusCode())
			.isEqualTo(HttpStatus.OK);
	}

	@Test
	void processZetaCallbackResponse_success() {
		ZetaWebhookDto<Object> zetaWebhookDto = new ZetaWebhookDto<>("id123", "1.0", java.time.Instant.now(), "status",
				"type", new Object(), null);
		given(this.zetaWebhookService.processZetaWebhook(zetaWebhookDto)).willReturn(1);

		ResponseEntity<?> response = this.webhookController.processZetaCallbackResponse(zetaWebhookDto);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(SUCCESS);
		then(this.zetaWebhookService).should().processZetaWebhook(zetaWebhookDto);
	}

	@Test
	void processAuthEvents_success() {
		AuthorizationPayload authorizationPayload = new AuthorizationPayload();
		ResponseEntity<?> response = this.webhookController.processAuthEvents(authorizationPayload);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(SUCCESS);
	}

}
