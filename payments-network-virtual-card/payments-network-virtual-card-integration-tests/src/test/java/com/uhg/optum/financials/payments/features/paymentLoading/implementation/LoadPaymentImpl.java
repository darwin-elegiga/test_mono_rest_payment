package com.uhg.optum.financials.payments.features.paymentLoading.implementation;

import com.fasterxml.jackson.databind.node.ObjectNode;
import com.uhg.optum.financials.payments.APITrigger.APIEndPointsImpl;
import com.uhg.optum.financials.payments.APITrigger.APITriggers;
import com.uhg.optum.financials.payments.common.CommonImpl;
import com.uhg.optum.financials.payments.payer.dto.CardCreationStatusDto;
import com.uhg.optum.financials.payments.payer.dto.PaymentResponseDto;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import okhttp3.ResponseBody;
import org.assertj.core.api.Assertions;
import retrofit2.Response;

import java.io.IOException;

public abstract class LoadPaymentImpl extends CommonImpl {

	private final APITriggers apiTriggers = APITriggers.getAPITriggersInstance();

	protected final APIEndPointsImpl apiEndPointsImpl = APIEndPointsImpl.getAPIEndPointsImplInstance();

	public void testLoadNewCardPaymentAndValidateStatusesAcrossEndpoints() throws IOException {
		PaymentRequestDto paymentRequestDto = prepareCardFlow(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		String paymentId = paymentRequestDto.getPaymentDetails().getId();

		PaymentResponseDto paymentDetails = this.apiEndPointsImpl.getPaymentDetails(paymentId);
		Assertions.assertThat(paymentDetails.getPaymentDetails().getId()).isEqualTo(paymentId);
		Assertions.assertThat(paymentDetails.getPaymentStatus()).isNotBlank();

		CardCreation cardCreationDetails = this.apiEndPointsImpl.getCardCreationDetails(paymentId);
		Assertions.assertThat(cardCreationDetails.getPaymentId()).isEqualTo(paymentId);
		Assertions.assertThat(cardCreationDetails.getStatus()).isEqualTo("CARD_ISSUED");

		CardCreationStatusDto cardPollingDetails = this.apiEndPointsImpl.awaitCardPollingDetails(paymentId);
		Assertions.assertThat(cardPollingDetails.getStatus()).isNotBlank();
		Assertions.assertThat(cardPollingDetails.getData()).isNotNull();
		Assertions.assertThat(cardPollingDetails.getData().getCardGuid()).isNotBlank();

		Response<ResponseBody> virtualCardDetailsResponse = this.apiTriggers.getVirtualCardDetails(paymentId);
		Assertions.assertThat(virtualCardDetailsResponse.code()).isEqualTo(200);
		Assertions.assertThat(virtualCardDetailsResponse.body()).isNotNull();
		ObjectNode virtualCardDetails = (ObjectNode) this.apiTriggers.objectMapper
			.readTree(virtualCardDetailsResponse.body().string());
		Assertions.assertThat(virtualCardDetails.path("paymentId").asText()).isEqualTo(paymentId);
		Assertions.assertThat(virtualCardDetails.path("cardGuid").asText())
			.isEqualTo(cardPollingDetails.getData().getCardGuid());
		Assertions.assertThat(virtualCardDetails.path("status").asText()).isNotBlank();
	}

}
