package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.CancelCardDto;
import com.uhg.optum.financials.payments.payer.repository.CardCancellationRepository;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.WebhookAlertUtil;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
public class VirtualCardCancelServiceImplTests {

	@InjectMocks
	VirtualCardCancelServiceImpl cancelVirtualCardService;

	@Mock
	LogUtility logUtility;

	@Mock
	CardCancellationRepository cardCancellationRepository;

	@Mock
	ZetaApiService zetaApiService;

	@Mock
	WebhookAlertUtil webhookAlertUtil;

	@Test
	public void testCancelVirtualCard() throws Exception {

		CancelCardDto cancelCardDto = new CancelCardDto();
		cancelCardDto.setPaymentId("123456");
		Assertions.assertThatRuntimeException()
			.isThrownBy(() -> this.cancelVirtualCardService.processCancelCardRequest(cancelCardDto));

	}

}
