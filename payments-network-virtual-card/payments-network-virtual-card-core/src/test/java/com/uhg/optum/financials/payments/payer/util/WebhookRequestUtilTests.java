package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.dto.Consolidated835FileDto;
import com.uhg.optum.financials.payments.payer.dto.Remit835SplitRequestDto;
import com.uhg.optum.financials.payments.payer.dto.authevent.MerchantInfo;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookBaseDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookPayload;
import com.uhg.optum.financials.payments.payer.dto.webhook.LoadingWebhookRequestPayload;
import com.uhg.optum.financials.payments.payer.dto.webhook.PaymentActivityWebhookRequestPayload;
import com.uhg.optum.financials.payments.payer.dto.webhook.WebhookBaseDto;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.Collections;
import java.util.HashMap;
import java.util.Map;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ACTIVE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUALCARD;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.ACTIVITY;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.DOT_DELIMITER;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.PAYMENT;

@ExtendWith(MockitoExtension.class)
class WebhookRequestUtilTests {

	@InjectMocks
	private WebhookRequestUtil webhookRequestUtil;

	@Test
	void testGetLoadingDto() {
		Assertions
			.assertThat(WebhookRequestUtil.getLoadingWebhookBaseDto("unload.failed", "paymentId", new HashMap<>(),
					"errorMessage", "errorCode", "networkPaymentId"))
			.isNotNull();
	}

	@Test
	void testPaymentNetworkMetadata() {
		Assertions.assertThat(
				WebhookRequestUtil.getPaymentNetworkMetaData(UUID.fromString("3fa85f64-5717-4562-b3fc-2c963f66afa4"),
						BigDecimal.ONE, BigDecimal.ONE, BigDecimal.ONE, "345677", "7865", "123456", ACTIVE))
			.isNotNull();
	}

	@Test
	void testGetCardEventWebhookBaseDto() {
		Map<String, String> thisMetadata = Collections.singletonMap("key", "value");
		CardEventWebhookBaseDto thisResult = WebhookRequestUtil.getCardEventWebhookBaseDto("type", "paymentId",
				thisMetadata, "networkPaymentId");
		Assertions.assertThat(thisResult).isNotNull();
		Assertions.assertThat(thisResult.getType()).isEqualTo("type");
		Assertions.assertThat(thisResult.getPaymentId()).isEqualTo("paymentId");
		Assertions.assertThat(thisResult.getNetworkPaymentId()).isEqualTo("networkPaymentId");
		Assertions.assertThat(thisResult.getNetworkMetadata()).isEqualTo(thisMetadata);
	}

	@Test
	void testGetCardEventWebhookPayload() {
		VirtualCard thisCard = new VirtualCard();
		thisCard.setCardGuid(UUID.randomUUID());
		MerchantInfo thisMerchant = new MerchantInfo();
		CardEventWebhookPayload thisPayload = WebhookRequestUtil.getCardEventWebhookPayload("123456", "987654", "type",
				"receiptId", BigDecimal.TEN, thisCard, thisMerchant, "approvalCode", "expiry", "respCode",
				"declineReason");
		Assertions.assertThat(thisPayload).isNotNull();
		Assertions.assertThat(thisPayload.getBin()).isEqualTo("123456");
		Assertions.assertThat(thisPayload.getLast4()).isEqualTo("7654");
		Assertions.assertThat(thisPayload.getId()).isEqualTo(thisCard.getCardGuid());
	}

	@Test
	void testGetRemittanceWebhook() {
		// Use realistic sample SAS URLs and orgShortCode
		String sasUrlRemit835File = "https://storage.example.com/remit835file.sas?sig=abc123";
		String sasUrlCompanion = "https://storage.example.com/companionfile.sas?sig=def456";
		String orgShortCode = "ORG001";

		Consolidated835FileDto thisDto = WebhookRequestUtil.getRemittanceWebhook(sasUrlRemit835File, sasUrlCompanion,
				orgShortCode);

		Assertions.assertThat(thisDto).isNotNull();
		Assertions.assertThat(thisDto.getOrigin()).isEqualTo(VIRTUALCARD);
		Assertions.assertThat(thisDto.getOrgShortCode()).isEqualTo(orgShortCode);
		Assertions.assertThat(thisDto.getRemit835FileInformation()).isNotNull();
		Assertions.assertThat(thisDto.getRemit835FileInformation().size()).isEqualTo(1);

		// Validate the contents of Remit835SplitRequestDto
		Remit835SplitRequestDto remitInfo = thisDto.getRemit835FileInformation().get(0);
		Assertions.assertThat(remitInfo.getRemit835FilePath()).isEqualTo(sasUrlRemit835File);
		Assertions.assertThat(remitInfo.getPaymentMappingFilePath()).isEqualTo(sasUrlCompanion);
	}

	@Test
	void testGetLoadingWebhookRequestPayload() {
		Double thisRedeemedAmount = 100.0;
		String thisAction = "LOAD";
		String thisActionDescription = "Load action";
		Double thisNewAvailableBalance = 200.0;
		LocalDate thisExpiryDate = LocalDate.of(2025, 11, 30);

		LoadingWebhookRequestPayload thisPayload = WebhookRequestUtil.getLoadingWebhookRequestPayload(
				thisRedeemedAmount, thisAction, thisActionDescription, thisNewAvailableBalance, thisExpiryDate);

		Assertions.assertThat(thisPayload).isNotNull();
		Assertions.assertThat(thisPayload.getDocuments()).isNull();
		Assertions.assertThat(thisPayload.getPaymentActivity()).isNotNull();
		Assertions.assertThat(thisPayload.getPaymentActivity().getRedeemedAmount()).isEqualTo(thisRedeemedAmount);
		Assertions.assertThat(thisPayload.getPaymentActivity().getAction()).isEqualTo(thisAction);
		Assertions.assertThat(thisPayload.getPaymentActivity().getActionDescription()).isEqualTo(thisActionDescription);
		Assertions.assertThat(thisPayload.getPaymentActivity().getNewAvailableBalance())
			.isEqualTo(thisNewAvailableBalance);
		Assertions.assertThat(thisPayload.getExpirationDate()).isEqualTo("2025-11-30");
		Assertions.assertThat(thisPayload.getNetworkId()).isEqualTo("VIRTUALCARD");
	}

	@Test
	void testGetPaymentActivityWebhookBaseDto() {
		Map<String, String> metadata = new HashMap<>();
		WebhookBaseDto webhookBaseDto = WebhookRequestUtil.getPaymentActivityWebhookBaseDto(
				PAYMENT + DOT_DELIMITER + ACTIVITY, "12345", metadata, null, null, "12345677");
		Assertions.assertThat(webhookBaseDto.getType()).isEqualTo(PAYMENT + DOT_DELIMITER + ACTIVITY);
		Assertions.assertThat(webhookBaseDto.getPaymentId()).isEqualTo("12345");
		Assertions.assertThat(webhookBaseDto.getNetworkPaymentId()).isEqualTo("12345677");

	}

	@Test
	void testGetPaymentActivityWebhookRequestPayload() {
		PaymentActivityWebhookRequestPayload paymentActivityWebhookRequestPayload = WebhookRequestUtil
			.getPaymentActivityWebhookRequestPayload(LocalDateTime.now(), BigDecimal.ZERO);
		Assertions.assertThat(paymentActivityWebhookRequestPayload.getNetworkId()).isEqualTo(VIRTUALCARD);
	}

}
