package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.model.PayeeDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.ShippingDetails;
import com.uhg.optum.financials.payments.payer.model.Address;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PayeeDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.ShippingDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.AddressDto;
import com.uhg.optum.financials.shared.client.dto.payment.ClaimsDto;
import com.uhg.optum.financials.shared.client.dto.payment.DocumentDto;
import com.uhg.optum.financials.shared.client.util.DateTimeUtil;
import org.springframework.stereotype.Component;
import java.util.ArrayList;
import java.util.List;
import java.util.stream.Collectors;

@Component
public class PaymentRequestDtoUtil {

	public PaymentRequestDto mapToDto(PaymentRequest paymentRequest) {
		if (paymentRequest == null) {
			return null;
		}

		PaymentRequestDto dto = new PaymentRequestDto();
		dto.setOrigin(paymentRequest.getOrigin());
		dto.setCorrelationId(paymentRequest.getCorrelationId());
		dto.setOrgShortCode(paymentRequest.getOrgShortCode());
		dto.setAccountShortCode(paymentRequest.getAccountShortCode());
		dto.setSubAccountShortCode(paymentRequest.getSubAccountShortCode());
		dto.setMetadata(paymentRequest.getMetadata());
		dto.setPaymentDetails(mapPaymentDetails(paymentRequest.getPaymentDetails()));
		dto.setPayeeDetails(mapPayeeDetails(paymentRequest.getPayeeDetails()));
		dto.setShippingDetails(mapShippingDetails(paymentRequest.getShippingDetails()));
		dto.setClaims(mapClaims(paymentRequest));
		dto.setDocuments(mapDocuments(paymentRequest.getDocuments()));
		dto.setRePreferenced(paymentRequest.isRePreferenceFlag());

		return dto;
	}

	private PaymentDetailsDto mapPaymentDetails(PaymentDetails paymentDetails) {
		if (paymentDetails == null) {
			return null;
		}

		return new PaymentDetailsDto(paymentDetails.getId(), paymentDetails.getIssuerPaymentId(),
				paymentDetails.getReferenceId(), DateTimeUtil.toYYYYMMDDDateWithHyphen(paymentDetails.getPaymentDate()),
				DateTimeUtil.toYYYYMMDDDateWithHyphen(paymentDetails.getAdjudicationCutoffDate()),
				paymentDetails.getAdjudicationVersion(), paymentDetails.getAmount(), paymentDetails.getRemitMethod(),
				paymentDetails.getJurisdictionalBody(), paymentDetails.getNumberOfAttachedPdfs(),
				paymentDetails.getSettlementTime(), paymentDetails.getType(), paymentDetails.getPin());
	}

	private PayeeDetailsDto mapPayeeDetails(PayeeDetails payeeDetails) {
		if (payeeDetails == null) {
			return null;
		}

		PayeeDetailsDto payeeDetailsDto = new PayeeDetailsDto();
		payeeDetailsDto.setType(payeeDetails.getType());
		payeeDetailsDto.setName(payeeDetails.getName());
		payeeDetailsDto.setAddress(mapAddress(payeeDetails.getAddress()));
		payeeDetailsDto.setTaxId(payeeDetails.getTaxId());
		payeeDetailsDto.setNpi(payeeDetails.getNpi());
		payeeDetailsDto.setReceiverId(payeeDetails.getReceiverId());

		return payeeDetailsDto;
	}

	private ShippingDetailsDto mapShippingDetails(ShippingDetails shippingDetails) {
		if (shippingDetails == null) {
			return null;
		}

		ShippingDetailsDto shippingDetailsDto = new ShippingDetailsDto();
		shippingDetailsDto.setName(shippingDetails.getName());
		shippingDetailsDto.setAddress(mapAddress(shippingDetails.getAddress()));

		return shippingDetailsDto;
	}

	private AddressDto mapAddress(Address address) {
		if (address == null) {
			return null;
		}

		return new AddressDto(address.getLine1(), address.getLine2(), address.getCity(), address.getState(),
				address.getCountry(), address.getPostalCode());
	}

	private ClaimsDto mapClaims(PaymentRequest paymentRequest) {
		return new ClaimsDto(paymentRequest.getTotalClmCount(), paymentRequest.getClmUrl(), paymentRequest.getDocType(),
				paymentRequest.isPlb());
	}

	private List<DocumentDto> mapDocuments(List<DocumentDto> documents) {
		if (documents == null || documents.isEmpty()) {
			return new ArrayList<>();
		}

		return documents.stream()
			.map((document) -> new DocumentDto(document.getDocType(), document.getUrl()))
			.collect(Collectors.toList());
	}

}
