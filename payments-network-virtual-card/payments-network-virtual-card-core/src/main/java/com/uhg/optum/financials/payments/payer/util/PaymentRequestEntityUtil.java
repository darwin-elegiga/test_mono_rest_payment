package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.enums.PayeeType;
import com.uhg.optum.financials.payments.payer.model.Address;
import com.uhg.optum.financials.payments.payer.model.PayeeDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.ShippingDetails;
import com.uhg.optum.financials.shared.client.dto.payment.AddressDto;
import com.uhg.optum.financials.shared.client.dto.payment.PayeeDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.client.dto.payment.ShippingDetailsDto;
import com.uhg.optum.financials.shared.client.util.DateTimeUtil;
import org.springframework.util.ObjectUtils;

import java.util.Locale;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;

public final class PaymentRequestEntityUtil {

	private static final long NETWORK_PAYMENT_ID_MIN_SEQUENCE = 2_821_109_907_456L; // base36:
																					// 100000000

	private static final long NETWORK_PAYMENT_ID_MAX_SEQUENCE = 101_559_956_668_415L; // base36:
																						// ZZZZZZZZZ

	private PaymentRequestEntityUtil() {
	}

	public static PaymentRequest toPaymentRequestEntity(PaymentRequestDto paymentRequestDto) {

		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setOrigin(paymentRequestDto.getOrigin());
		paymentRequest.setOrgShortCode(paymentRequestDto.getOrgShortCode());
		paymentRequest.setAccountShortCode(paymentRequestDto.getAccountShortCode());
		paymentRequest.setSubAccountShortCode(paymentRequestDto.getSubAccountShortCode());
		paymentRequest.setCorrelationId(paymentRequestDto.getCorrelationId());
		if (!ObjectUtils.isEmpty(paymentRequestDto.getPaymentDetails())) {
			paymentRequest.setPaymentDetails(toPaymentDetailsEntity(paymentRequestDto.getPaymentDetails()));
		}

		if (!ObjectUtils.isEmpty(paymentRequestDto.getPayeeDetails())) {
			paymentRequest.setPayeeDetails(toPayeeDetailsEntity(paymentRequestDto.getPayeeDetails()));
		}

		if (!ObjectUtils.isEmpty(paymentRequestDto.getShippingDetails())) {
			paymentRequest.setShippingDetails(toShippingDetailsEntity(paymentRequestDto.getShippingDetails()));
		}

		if (!ObjectUtils.isEmpty(paymentRequestDto.getClaims())) {
			paymentRequest.setTotalClmCount(paymentRequestDto.getClaims().getTotalCount());
			paymentRequest.setClmUrl(paymentRequestDto.getClaims().getUrl());
			paymentRequest.setDocType(paymentRequestDto.getClaims().getDocType());
			paymentRequest.setPlb(paymentRequestDto.getClaims().isPlb());
		}

		if (!ObjectUtils.isEmpty(paymentRequestDto.getDocuments())) {
			paymentRequest.setDocuments(paymentRequestDto.getDocuments());
		}
		if (!ObjectUtils.isEmpty(paymentRequestDto.getMetadata())) {
			paymentRequest.setMetadata(paymentRequestDto.getMetadata());
		}
		paymentRequest.setRePreferenceFlag(resolveRePreferenceFlag(paymentRequestDto));
		paymentRequest.setRePreferenceProcessed(false);
		return paymentRequest;
	}

	private static PaymentDetails toPaymentDetailsEntity(PaymentDetailsDto paymentDetailsDto) {
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId(paymentDetailsDto.getId());
		paymentDetails.setIssuerPaymentId(paymentDetailsDto.getIssuerPaymentId());
		paymentDetails.setReferenceId(paymentDetailsDto.getReferenceId());
		paymentDetails.setPaymentDate(DateTimeUtil.toLocalDateYYYYMMDDHyphenFormat(paymentDetailsDto.getPaymentDate()));
		if (paymentDetailsDto.getAdjudicationCutoffDate() != null
				&& !paymentDetailsDto.getAdjudicationCutoffDate().isEmpty()) {
			paymentDetails.setAdjudicationCutoffDate(
					DateTimeUtil.toLocalDateYYYYMMDDHyphenFormat(paymentDetailsDto.getAdjudicationCutoffDate()));
		}
		paymentDetails.setAdjudicationVersion(paymentDetailsDto.getAdjudicationVersion());
		paymentDetails.setAmount(paymentDetailsDto.getAmount());
		paymentDetails.setRemitMethod(paymentDetailsDto.getRemitMethod());
		paymentDetails.setJurisdictionalBody(paymentDetailsDto.getJurisdictionalBody());
		paymentDetails.setNumberOfAttachedPdfs(paymentDetailsDto.getNumberOfAttachedPdfs());
		paymentDetails.setSettlementTime(paymentDetailsDto.getSettlementTime());
		paymentDetails.setType(paymentDetailsDto.getType());
		paymentDetails.setPin(paymentDetailsDto.getPin());
		paymentDetails.setCreatedById(VIRTUAL_CARD);
		return paymentDetails;
	}

	public static String generateNetworkPaymentId(String payeeType, long sequenceValue) {
		if (sequenceValue < NETWORK_PAYMENT_ID_MIN_SEQUENCE || sequenceValue > NETWORK_PAYMENT_ID_MAX_SEQUENCE) {
			throw new IllegalArgumentException("Sequence value must be between 2821109907456 and 101559956668415");
		}
		String base36Value = Long.toString(sequenceValue, 36).toUpperCase(Locale.ROOT);
		return PayeeType.prefixFromInput(payeeType) + String.format("%9s", base36Value).replace(' ', '0');
	}

	private static PayeeDetails toPayeeDetailsEntity(PayeeDetailsDto payeeDetailsDto) {
		PayeeDetails payeeDetails = new PayeeDetails();
		payeeDetails.setType(payeeDetailsDto.getType());
		payeeDetails.setName(payeeDetailsDto.getName());
		if (!ObjectUtils.isEmpty(payeeDetailsDto.getAddress())) {
			payeeDetails.setAddress(toAddressEntity(payeeDetailsDto.getAddress()));
		}

		payeeDetails.setTaxId(payeeDetailsDto.getTaxId());
		payeeDetails.setNpi(payeeDetailsDto.getNpi());
		payeeDetails.setReceiverId(payeeDetailsDto.getReceiverId());
		return payeeDetails;
	}

	private static ShippingDetails toShippingDetailsEntity(ShippingDetailsDto shippingDetailsDto) {
		ShippingDetails shippingDetails = new ShippingDetails();
		shippingDetails.setName(shippingDetailsDto.getName());
		if (!ObjectUtils.isEmpty(shippingDetailsDto.getAddress())
				&& !checkNullShippingDtoAddress(shippingDetailsDto.getAddress())) {
			shippingDetails.setAddress(toAddressEntity(shippingDetailsDto.getAddress()));
		}
		return shippingDetails;
	}

	private static Address toAddressEntity(AddressDto addressDto) {
		Address address = new Address();
		address.setLine1(addressDto.getLine1());
		address.setLine2(addressDto.getLine2());
		address.setCity(addressDto.getCity());
		address.setState(addressDto.getState());
		address.setCountry(addressDto.getCountry());
		address.setPostalCode(addressDto.getPostalCode());
		return address;
	}

	public static boolean checkNullShippingDtoAddress(AddressDto shippingDetailsAddressDto) {
		return ((shippingDetailsAddressDto.getCity() == null || shippingDetailsAddressDto.getCity().isBlank())
				&& (shippingDetailsAddressDto.getCountry() == null || shippingDetailsAddressDto.getCountry().isBlank())
				&& (shippingDetailsAddressDto.getLine1() == null || shippingDetailsAddressDto.getLine1().isBlank())
				&& (shippingDetailsAddressDto.getLine2() == null || shippingDetailsAddressDto.getLine2().isBlank())
				&& (shippingDetailsAddressDto.getPostalCode() == null
						|| shippingDetailsAddressDto.getPostalCode().isBlank()));
	}

	public static boolean resolveRePreferenceFlag(PaymentRequestDto requestDto) {
		return requestDto != null && Boolean.TRUE.equals(requestDto.isRePreferenced());
	}

}
