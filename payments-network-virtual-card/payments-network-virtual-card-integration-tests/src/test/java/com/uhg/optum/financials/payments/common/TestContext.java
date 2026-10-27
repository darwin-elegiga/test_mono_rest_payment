package com.uhg.optum.financials.payments.common;

import java.util.UUID;

public record TestContext(UUID orgId, String orgShortCode, String accountShortCode, String subAccountShortCode,
		UUID bankAccountId) {
}
