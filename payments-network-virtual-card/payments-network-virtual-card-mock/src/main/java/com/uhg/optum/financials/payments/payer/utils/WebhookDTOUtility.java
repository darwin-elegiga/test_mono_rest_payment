package com.uhg.optum.financials.payments.payer.utils;

import com.uhg.optum.financials.payments.payer.dto.authevent.AuthorizationPayload;
import com.uhg.optum.financials.payments.payer.dto.authevent.FraudEngineResponse;
import com.uhg.optum.financials.payments.payer.dto.authevent.FraudInfo;
import com.uhg.optum.financials.payments.payer.dto.authevent.MerchantInfo;
import com.uhg.optum.financials.payments.payer.dto.authevent.Money;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.time.Instant;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.time.temporal.ChronoUnit;
import java.util.List;
import java.util.concurrent.ThreadLocalRandom;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.EventTypeConstants.AUTH_EVENT;

public final class WebhookDTOUtility {

	private static final int MAX_RECEIPT_ID_LEN = 20;

	private static final int MAX_RETRIEVAL_REFERENCE_LEN = 50;

	private static final int MAX_ORIGINAL_AUTH_REQ_ID_LEN = 256;

	private static final int MAX_APPROVAL_CODE_LEN = 6;

	private WebhookDTOUtility() {
	}

	public static ZetaWebhookDto<AuthorizationPayload> createAuthZetaWebhookDto(String cardId, String receiptId,
			BigDecimal amountInDollars) {
		MerchantInfo merchantInfo = new MerchantInfo();
		merchantInfo.setMerchantId("2001");
		merchantInfo.setMerchantTerminalId("TID001");
		merchantInfo.setMerchantCategoryCode("5411");
		merchantInfo.setMerchantName("TEXMART");
		merchantInfo.setMerchantCity("LOSANGELES");
		merchantInfo.setMerchantCountry("US");

		FraudEngineResponse fraudEngineResponse = new FraudEngineResponse();
		fraudEngineResponse.setAdvice("ACCEPT");
		fraudEngineResponse.setTriggeredRules(List.of());
		fraudEngineResponse.setRuleTags(List.of());

		FraudInfo fraudInfo = new FraudInfo();
		fraudInfo.setFraudEngineAdvice("PASS_THROUGH");
		fraudInfo.setFraudEngineResponseType("CLIENT_RESPONSE");
		fraudInfo.setFraudEngineResponse(fraudEngineResponse);

		Money money = new Money();
		money.setValue(toMinorUnits(amountInDollars));
		money.setCurrency("USD");

		AuthorizationPayload payload = new AuthorizationPayload();
		payload.setReceiptId(limit(generateReceiptId(receiptId), MAX_RECEIPT_ID_LEN));
		payload.setRetrievalReferenceNumber(limit(randomDigits(12), MAX_RETRIEVAL_REFERENCE_LEN));
		payload.setOriginalAuthRequestId(limit(randomAlphaNumeric(16) + "_" + cardId, MAX_ORIGINAL_AUTH_REQ_ID_LEN));
		payload.setApprovalCode(limit(randomAlphaNumeric(6), MAX_APPROVAL_CODE_LEN));
		payload.setSystemAuthTime(OffsetDateTime.now(ZoneOffset.UTC).truncatedTo(ChronoUnit.SECONDS).toString());
		payload
			.setAuthExpiryTime(Instant.now().plus(15, ChronoUnit.MINUTES).truncatedTo(ChronoUnit.SECONDS).toString());
		payload.setIfiId("600710");
		payload.setCardId(cardId);
		payload.setPaymentNetwork("MASTERCARD");
		payload.setBin("111111");
		payload.setMaskedCardNumber("111111-xxxxxx-0307");
		payload.setMti("0100");
		payload.setAuthAmount(money);
		payload.setAcquiringInstitutionId("968500");
		payload.setMerchantInfo(merchantInfo);
		payload.setPaymentCode("PMCUSZZ112910034");
		payload.setPaymentCodeName("INT_ECOM_PURCHASE_UNSECURE");
		payload.setChannelCode("CHNUSZZ1129");
		payload.setChannelCodeName("MCB2BUSOffusChannel");
		payload.setTransactionStatus("APPROVED");
		payload.setFraudInfo(fraudInfo);

		return new ZetaWebhookDto<>(UUID.randomUUID().toString(), "1.0", Instant.now(), "SUCCESS", AUTH_EVENT, payload,
				null);
	}

	public static String generateReceiptId() {
		return randomAlphaNumeric(MAX_RECEIPT_ID_LEN);
	}

	private static String toMinorUnits(BigDecimal amountInDollars) {
		if (amountInDollars == null || amountInDollars.signum() <= 0) {
			return "0";
		}
		return amountInDollars.movePointRight(2).setScale(0, RoundingMode.DOWN).toPlainString();
	}

	private static String randomDigits(int length) {
		StringBuilder sb = new StringBuilder(length);
		for (int i = 0; i < length; i++) {
			sb.append(ThreadLocalRandom.current().nextInt(10));
		}
		return sb.toString();
	}

	private static String randomAlphaNumeric(int length) {
		final String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		StringBuilder sb = new StringBuilder(length);
		for (int i = 0; i < length; i++) {
			sb.append(chars.charAt(ThreadLocalRandom.current().nextInt(chars.length())));
		}
		return sb.toString();
	}

	private static String generateReceiptId(String receiptId) {
		return (receiptId == null || receiptId.isBlank()) ? generateReceiptId() : receiptId;
	}

	private static String limit(String value, int maxLength) {
		if (value == null) {
			return null;
		}
		return (value.length() <= maxLength) ? value : value.substring(0, maxLength);
	}

}
