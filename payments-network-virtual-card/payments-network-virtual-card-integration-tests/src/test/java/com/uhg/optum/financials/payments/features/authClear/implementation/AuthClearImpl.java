package com.uhg.optum.financials.payments.features.authClear.implementation;

import com.uhg.optum.financials.payments.common.CommonImpl;
import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import org.assertj.core.api.Assertions;

import java.io.IOException;
import java.time.LocalDate;

public abstract class AuthClearImpl extends CommonImpl {

	private static final int NUMBER_OF_AUTH_EVENTS = 1;

	protected AuthClearScenario prepareAuthorizedCard(LocalDate paymentDate) throws IOException {
		PaymentRequestDto paymentRequestDto = prepareCardFlow(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		String cardId = getCardPollingDetails(paymentRequestDto.getPaymentDetails().getId()).getData().getCardGuid();
		String receiptId = generateAuth(cardId, paymentDate);
		return new AuthClearScenario(paymentRequestDto, receiptId);
	}

	protected AuthExtract generateClearExtractAndAssertClearingStatus(String receiptId, LocalDate paymentDate,
			String expectedClearingStatus) throws IOException {
		apiEndPointsImpl.generateClearExtractByDateRange(paymentDate);
		apiEndPointsImpl.awaitAuthExtractClearingStatus(receiptId, expectedClearingStatus);
		return assertAuthExtractClearingStatus(receiptId, expectedClearingStatus);
	}

	protected AuthExtract assertAuthExtractClearingStatus(String receiptId, String expectedClearingStatus)
			throws IOException {
		AuthExtract authExtract = apiEndPointsImpl.getAuthExtractRecord(receiptId);
		Assertions.assertThat(authExtract.getReceiptId()).isEqualTo(receiptId);
		Assertions.assertThat(authExtract.getClearingStatus()).isEqualTo(expectedClearingStatus);
		return authExtract;
	}

	protected void resetCard(PaymentRequestDto paymentRequestDto, String receiptId, int expectedStatus)
			throws IOException {
		apiEndPointsImpl.resetCardRequest(buildResetCardRequest(paymentRequestDto, receiptId),
				paymentRequestDto.getPaymentDetails().getId(), expectedStatus);
	}

	private String generateAuth(String cardId, LocalDate paymentDate) throws IOException {
		String receiptId = apiEndPointsImpl.generateAuthEventReceiptId(paymentDate, NUMBER_OF_AUTH_EVENTS, cardId);
		apiEndPointsImpl.awaitAuthExtractRecord(receiptId);
		return receiptId;
	}

	private ResetCardRequest buildResetCardRequest(PaymentRequestDto paymentRequestDto, String receiptId) {
		ResetCardRequest resetCardRequest = new ResetCardRequest();
		resetCardRequest.setResetPurpose("Test");
		resetCardRequest.setPaymentId(paymentRequestDto.getPaymentDetails().getId());
		resetCardRequest.setReceiptId(receiptId);
		return resetCardRequest;
	}

	protected record AuthClearScenario(PaymentRequestDto paymentRequestDto, String receiptId) {
	}

}
