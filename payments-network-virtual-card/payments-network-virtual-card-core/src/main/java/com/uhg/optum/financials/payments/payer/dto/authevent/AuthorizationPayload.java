package com.uhg.optum.financials.payments.payer.dto.authevent;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class AuthorizationPayload {

	private String receiptId;

	private String originalAuthRequestId;

	private String retrievalReferenceNumber;

	private String approvalCode;

	private String systemAuthTime;

	private String authExpiryTime;

	private String ifiId;

	private String cardId;

	private String paymentNetwork;

	private String bin;

	private String maskedCardNumber;

	private String mti;

	private Money authAmount;

	private String acquiringInstitutionId;

	private MerchantInfo merchantInfo;

	private String paymentCode;

	private String paymentCodeName;

	private String channelCode;

	private String channelCodeName;

	private String transactionStatus;

	private String transactionType;

	private String declineReason;

	private String declineErrorCode;

	private FraudInfo fraudInfo;

}
