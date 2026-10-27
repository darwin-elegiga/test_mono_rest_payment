package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import com.uhg.optum.financials.payments.payer.repository.CardResetRepository;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.WebhookAlertUtil;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
public class VirtualCardResetServiceImplTests {

	@InjectMocks
	VirtualCardResetServiceImpl virtualCardResetService;

	@Mock
	LogUtility logUtility;

	@Mock
	CardResetRepository cardResetRepository;

	@Mock
	ZetaApiService zetaApiService;

	@Mock
	AuditLogger auditLogger;

	@Mock
	WebhookAlertUtil webhookAlertUtil;

	@Test
	public void testReset() throws Exception {
		ResetCardRequest resetCardRequest = new ResetCardRequest();
		Assertions.assertThatRuntimeException()
			.isThrownBy(() -> this.virtualCardResetService.processResetCardRequest(resetCardRequest));
		Mockito.verify(this.cardResetRepository, Mockito.times(1)).save(any());
	}

}
