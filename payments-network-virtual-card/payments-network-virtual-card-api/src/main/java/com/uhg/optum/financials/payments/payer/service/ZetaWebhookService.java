package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;

public interface ZetaWebhookService {

	int processZetaWebhook(ZetaWebhookDto zetaWebhookDto);

}
