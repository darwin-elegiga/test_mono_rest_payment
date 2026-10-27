package com.uhg.optum.financials.payments.features.cardCancellation.implementation;

import com.uhg.optum.financials.payments.common.CommonImpl;
import com.uhg.optum.financials.payments.payer.dto.CancelCardDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import org.json.JSONException;

import java.io.IOException;

public abstract class CardCancellationImpl extends CommonImpl {

	protected CardCancellationScenario prepareCardForCancellation() throws IOException {
		PaymentRequestDto paymentRequestDto = prepareCardFlow(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		return new CardCancellationScenario(paymentRequestDto);
	}

	protected void cancelCard(CardCancellationScenario scenario, String requestPaymentId, String reasonCode,
			int expectedStatus) throws IOException {
		apiEndPointsImpl.cancelCard(buildCancelCardDto(requestPaymentId, reasonCode), scenario.paymentId(),
				expectedStatus);
	}

	protected void cancelCardAndAssertStatus(CardCancellationScenario scenario, String reasonCode)
			throws IOException, JSONException {
		cancelCard(scenario, scenario.paymentId(), reasonCode, 200);
		apiEndPointsImpl.getCancelCardDetails(scenario.paymentId(), "CARD_CANCELLED", reasonCode);
	}

	private CancelCardDto buildCancelCardDto(String paymentId, String reasonCode) {
		CancelCardDto cancelCardDto = new CancelCardDto();
		cancelCardDto.setPaymentId(paymentId);
		cancelCardDto.setReasonCode(reasonCode);
		return cancelCardDto;
	}

	protected record CardCancellationScenario(PaymentRequestDto paymentRequestDto) {

		protected String paymentId() {
			return this.paymentRequestDto.getPaymentDetails().getId();
		}

	}

}
