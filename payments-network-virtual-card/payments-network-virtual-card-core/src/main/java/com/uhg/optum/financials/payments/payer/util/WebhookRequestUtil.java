package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.dto.Consolidated835FileDto;
import com.uhg.optum.financials.payments.payer.dto.Remit835SplitRequestDto;
import com.uhg.optum.financials.payments.payer.dto.authevent.MerchantInfo;
import com.uhg.optum.financials.payments.payer.dto.webhook.AuthorizationWebhookPayload;
import com.uhg.optum.financials.payments.payer.dto.webhook.Balance;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookBaseDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookPayload;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardWebhookDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.LoadingWebhookRequestDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.LoadingWebhookRequestPayload;
import com.uhg.optum.financials.payments.payer.dto.webhook.MerchantPayload;
import com.uhg.optum.financials.payments.payer.dto.webhook.PaymentActivityPayload;
import com.uhg.optum.financials.payments.payer.dto.webhook.PaymentActivityWebhookRequestDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.PaymentActivityWebhookRequestPayload;
import com.uhg.optum.financials.payments.payer.dto.webhook.PaymentNetworkMetadataDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.WebhookBaseDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.WebhookErrorPayload;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.Date;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ACTIVE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD_AUTH_EVENT_APPROVED;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD_AUTH_EVENT_DECLINED;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD_AUTH_REVERSED;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD_AUTH_REVERSED_FAILED;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CREDIT;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.DEBIT;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.PAYMENT_METHOD;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUALCARD;

public final class WebhookRequestUtil {

	private WebhookRequestUtil() {
		// private constructor
	}

	public static WebhookBaseDto getLoadingWebhookBaseDto(String type, String paymentId, Map<String, String> metadata,
			String errorMessage, String errorCode, String networkPaymentId) {

		WebhookBaseDto loadingWebhookRequestDto = new LoadingWebhookRequestDto();
		loadingWebhookRequestDto.setType(type);
		loadingWebhookRequestDto.setPaymentId(paymentId);
		loadingWebhookRequestDto.setTimestamp(new Date().toInstant().toString());
		loadingWebhookRequestDto.setNetworkPaymentId(networkPaymentId);
		if (errorMessage != null || errorCode != null) {
			WebhookErrorPayload errorPayload = new WebhookErrorPayload();
			errorPayload.setMessage(errorMessage);
			errorPayload.setCode(errorCode);
			loadingWebhookRequestDto.setError(errorPayload);
		}
		loadingWebhookRequestDto.setMetadata(metadata);

		return loadingWebhookRequestDto;
	}

	public static PaymentNetworkMetadataDto getPaymentNetworkMetaData(UUID cardGuid, BigDecimal initialAmount,
			BigDecimal currentAmount, BigDecimal availableAmount, String bin, String last4, String receiptId,
			String status) {
		PaymentNetworkMetadataDto networkMetadata = new PaymentNetworkMetadataDto();
		networkMetadata.setModality(CARD);
		CardWebhookDto cardWebhookDto = new CardWebhookDto();
		cardWebhookDto.setId(cardGuid);
		cardWebhookDto.setBin(bin);
		Balance balance = new Balance();
		balance.setAvailable(availableAmount);
		balance.setCurrent(currentAmount);
		balance.setInitial(initialAmount);
		cardWebhookDto.setBalance(balance);
		cardWebhookDto.setStatus(status);
		if (last4 != null) {
			cardWebhookDto.setLast4(last4.substring(last4.length() - 4));
		}
		else {
			cardWebhookDto.setLast4(null);
		}
		cardWebhookDto.setReceiptId(receiptId);
		cardWebhookDto.setApprovalCode(null);
		cardWebhookDto.setAccountingTransaction(null);
		cardWebhookDto.setMerchantName(null);
		cardWebhookDto.setMerchantTerminalId(null);
		networkMetadata.setCard(cardWebhookDto);
		return networkMetadata;
	}

	public static CardEventWebhookBaseDto getCardEventWebhookBaseDto(String type, String paymentId,
			Map<String, String> metadata, String networkPaymentId) {

		CardEventWebhookBaseDto cardIssuedWebhookBaseDto = new CardEventWebhookBaseDto();
		cardIssuedWebhookBaseDto.setType(type);
		cardIssuedWebhookBaseDto.setPaymentId(paymentId);
		cardIssuedWebhookBaseDto.setTimestamp(new Date().toInstant().toString());
		cardIssuedWebhookBaseDto.setNetworkPaymentId(networkPaymentId);
		cardIssuedWebhookBaseDto.setNetworkMetadata(metadata);
		cardIssuedWebhookBaseDto.setModality(CARD);
		cardIssuedWebhookBaseDto.setPublic(true);
		cardIssuedWebhookBaseDto.setContainsPCI(false);
		cardIssuedWebhookBaseDto.setNetworkId(VIRTUALCARD);
		return cardIssuedWebhookBaseDto;
	}

	public static CardEventWebhookPayload getCardEventWebhookPayload(String bin, String last4, String type,
			String receiptId, BigDecimal authAmount, VirtualCard virtualCard, MerchantInfo merchantInfo,
			String approvalCode, String authExpiryTime, String responseCode, String declineReason) {
		CardEventWebhookPayload payload = new CardEventWebhookPayload();
		payload.setId(virtualCard.getCardGuid());
		payload.setBin(bin);
		if (last4 != null && last4.length() >= 4) {
			payload.setLast4(last4.substring(last4.length() - 4));
		}
		else {
			payload.setLast4((last4 != null) ? last4 : "");
		}
		payload.setStatus(ACTIVE);
		Balance balance = new Balance();
		balance.setAvailable(virtualCard.getAvailableBalance());
		balance.setCurrent(virtualCard.getCurrentBalance());
		balance.setInitial(virtualCard.getInitialBalance());
		payload.setBalance(balance);
		AuthorizationWebhookPayload authorization = getAuthorizationPayload(type, receiptId, authAmount, approvalCode,
				authExpiryTime, responseCode, declineReason, merchantInfo);
		payload.setAuthorization(authorization);
		return payload;
	}

	private static AuthorizationWebhookPayload getAuthorizationPayload(String type, String receiptId,
			BigDecimal authAmount, String approvalCode, String authExpiryTime, String responseCode,
			String declineReason, MerchantInfo merchantInfo) {
		AuthorizationWebhookPayload authorization = new AuthorizationWebhookPayload();

		if (CARD_AUTH_REVERSED.equalsIgnoreCase(type)) {
			authorization.setReceiptId(receiptId);
			authorization.setAuthAmount(authAmount);
			authorization.setAccountingTransaction(CREDIT);
		}
		else if (CARD_AUTH_REVERSED_FAILED.equalsIgnoreCase(type)) {
			authorization.setReceiptId(receiptId);
			authorization.setAuthAmount(authAmount);
			authorization.setAccountingTransaction(CREDIT);
			authorization.setDeclineReason(declineReason);
		}
		else if (CARD_AUTH_EVENT_APPROVED.equalsIgnoreCase(type) || CARD_AUTH_EVENT_DECLINED.equalsIgnoreCase(type)) {
			authorization.setReceiptId(receiptId);
			authorization.setAuthAmount(authAmount);
			authorization.setAccountingTransaction(DEBIT);
			authorization.setApprovalCode(approvalCode);
			authorization.setAuthExpiryTime(authExpiryTime);
			authorization.setDeclineReason(declineReason);
			authorization.setResponseCode(responseCode);
			MerchantPayload merchant = getMerchantPayload(merchantInfo);
			authorization.setMerchant(merchant);
		}
		else {
			authorization.setReceiptId(null);
			authorization.setApprovalCode(null);
			authorization.setAuthExpiryTime(null);
			authorization.setAccountingTransaction(null);
			authorization.setAuthAmount(BigDecimal.ZERO);
			authorization.setDeclineReason(null);
			authorization.setResponseCode(null);
			MerchantPayload merchant = getMerchantPayload(merchantInfo);
			authorization.setMerchant(merchant);
		}

		return authorization;
	}

	private static MerchantPayload getMerchantPayload(MerchantInfo merchantInfo) {
		MerchantPayload merchant = new MerchantPayload();
		if (merchantInfo != null) {
			merchant.setName(merchantInfo.getMerchantName());
			merchant.setTerminalId(merchantInfo.getMerchantTerminalId());
			merchant.setCity(merchantInfo.getMerchantCity());
			merchant.setState(merchantInfo.getMerchantState());
			merchant.setCategoryCode(merchantInfo.getMerchantCategoryCode());
		}
		else {
			merchant.setName(null);
			merchant.setTerminalId(null);
			merchant.setCity(null);
			merchant.setState(null);
			merchant.setCategoryCode(null);
		}
		return merchant;
	}

	public static Consolidated835FileDto getRemittanceWebhook(String sasUrlRemit835File, String sasUrlCompanion,
			String orgShortCode) {
		Consolidated835FileDto consolidated835FileList = new Consolidated835FileDto();
		consolidated835FileList.setOrigin(VIRTUALCARD);
		consolidated835FileList.setOrgShortCode(orgShortCode);
		Remit835SplitRequestDto remit835SplitRequestDto = new Remit835SplitRequestDto();
		remit835SplitRequestDto.setRemit835FilePath(sasUrlRemit835File);
		remit835SplitRequestDto.setPaymentMappingFilePath(sasUrlCompanion);
		consolidated835FileList.setRemit835FileInformation(List.of(remit835SplitRequestDto));
		return consolidated835FileList;
	}

	public static LoadingWebhookRequestPayload getLoadingWebhookRequestPayload(Double redeemedAmount, String action,
			String actionDescription, Double newAvailableBalance, LocalDate expiryDate) {

		LoadingWebhookRequestPayload payload = new LoadingWebhookRequestPayload();
		payload.setDocuments(null);
		PaymentActivityPayload paymentActivityPayload = new PaymentActivityPayload();
		paymentActivityPayload.setRedeemedAmount(redeemedAmount);
		paymentActivityPayload.setAction(action);
		paymentActivityPayload.setActionDescription(actionDescription);
		paymentActivityPayload.setNewAvailableBalance(newAvailableBalance);
		payload.setPaymentActivity(paymentActivityPayload);
		payload.setExpirationDate((expiryDate != null) ? expiryDate.toString() : null);
		payload.setNetworkId(VIRTUALCARD);
		return payload;
	}

	public static WebhookBaseDto getPaymentActivityWebhookBaseDto(String type, String paymentId,
			Map<String, String> metadata, String errorMessage, String errorCode, String networkPaymentId) {

		WebhookBaseDto loadingWebhookRequestDto = new PaymentActivityWebhookRequestDto();
		loadingWebhookRequestDto.setType(type);
		loadingWebhookRequestDto.setPaymentId(paymentId);
		loadingWebhookRequestDto.setTimestamp(new Date().toInstant().toString());
		loadingWebhookRequestDto.setNetworkPaymentId(networkPaymentId);
		if (errorMessage != null || errorCode != null) {
			WebhookErrorPayload errorPayload = new WebhookErrorPayload();
			errorPayload.setMessage(errorMessage);
			errorPayload.setCode(errorCode);
			loadingWebhookRequestDto.setError(errorPayload);
		}
		loadingWebhookRequestDto.setMetadata(metadata);
		return loadingWebhookRequestDto;
	}

	public static PaymentActivityWebhookRequestPayload getPaymentActivityWebhookRequestPayload(
			LocalDateTime processedDate, BigDecimal amount) {

		PaymentActivityWebhookRequestPayload paymentActivityWebhookRequestPayload = new PaymentActivityWebhookRequestPayload();
		paymentActivityWebhookRequestPayload.setStateMetadata(new HashMap<>());
		paymentActivityWebhookRequestPayload.setNetworkId(VIRTUALCARD);
		paymentActivityWebhookRequestPayload.setPaymentMethod(PAYMENT_METHOD);
		paymentActivityWebhookRequestPayload.setCheckNumber("");
		paymentActivityWebhookRequestPayload.setSettledDate(processedDate.toLocalDate().toString());
		paymentActivityWebhookRequestPayload.setNorCode("");
		paymentActivityWebhookRequestPayload.setVcpStatus("Issued");
		paymentActivityWebhookRequestPayload.setConsolidatedPaymentAmount(amount.doubleValue());
		paymentActivityWebhookRequestPayload.setReoriginationDate(null);
		paymentActivityWebhookRequestPayload.setRevisedPaymentMethod(null);
		return paymentActivityWebhookRequestPayload;
	}

}
