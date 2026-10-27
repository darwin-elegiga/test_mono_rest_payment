package com.uhg.optum.financials.payments.features.cardReset.implementation;

import com.uhg.optum.financials.payments.common.CommonImpl;
import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import org.assertj.core.api.Assertions;

import java.io.IOException;
import java.time.LocalDate;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.AUTH_RESET_CLEAR;

public abstract class CardResetImpl extends CommonImpl {

	private static final int NUMBER_OF_AUTH_EVENTS = 1;

	protected CardResetScenario prepareCardForReset() throws IOException {
		PaymentRequestDto paymentRequestDto = prepareCardFlow(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		String cardId = getCardPollingDetails(paymentRequestDto.getPaymentDetails().getId()).getData().getCardGuid();
		String receiptId = generateReceiptIdForReset(cardId);
		return new CardResetScenario(paymentRequestDto, receiptId);
	}

	protected void resetCard(CardResetScenario scenario, String resetReason, int expectedStatus) throws IOException {
		resetCard(scenario, resetReason, scenario.paymentId(), expectedStatus);
	}

	protected void resetCard(CardResetScenario scenario, String resetReason, String requestPaymentId,
			int... expectedStatuses) throws IOException {
		apiEndPointsImpl.resetCardRequest(buildResetCardRequest(resetReason, requestPaymentId, scenario.receiptId()),
				scenario.paymentId(), expectedStatuses);
	}

	protected void assertAuthResetClearStatus(String receiptId) throws IOException {
		AuthExtract authExtract = apiEndPointsImpl.getAuthExtractRecord(receiptId);
		Assertions.assertThat(authExtract.getReceiptId()).isEqualTo(receiptId);
		Assertions.assertThat(authExtract.getClearingStatus()).isEqualTo(AUTH_RESET_CLEAR);
	}

	private String generateReceiptIdForReset(String cardId) throws IOException {
		String receiptId = apiEndPointsImpl.generateAuthEventReceiptId(LocalDate.now(), NUMBER_OF_AUTH_EVENTS, cardId);
		apiEndPointsImpl.awaitAuthExtractRecord(receiptId);
		return receiptId;
	}

	private ResetCardRequest buildResetCardRequest(String resetReason, String paymentId, String receiptId) {
		ResetCardRequest resetCardRequest = new ResetCardRequest();
		resetCardRequest.setResetPurpose(resetReason);
		resetCardRequest.setPaymentId(paymentId);
		resetCardRequest.setReceiptId(receiptId);
		return resetCardRequest;
	}

	protected record CardResetScenario(PaymentRequestDto paymentRequestDto, String receiptId) {

		protected String paymentId() {
			return this.paymentRequestDto.getPaymentDetails().getId();
		}

	}

}
