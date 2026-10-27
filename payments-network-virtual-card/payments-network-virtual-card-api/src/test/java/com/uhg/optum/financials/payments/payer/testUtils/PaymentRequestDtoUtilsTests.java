package com.uhg.optum.financials.payments.payer.testUtils;

import com.uhg.optum.financials.shared.client.constant.PayeeType;
import com.uhg.optum.financials.shared.client.constant.RemitMethod;
import com.uhg.optum.financials.shared.client.dto.payment.AddressDto;
import com.uhg.optum.financials.shared.client.dto.payment.ClaimsDto;
import com.uhg.optum.financials.shared.client.dto.payment.DocumentDto;
import com.uhg.optum.financials.shared.client.dto.payment.PayeeDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.client.dto.payment.ShippingDetailsDto;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.junit.jupiter.MockitoExtension;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;

import static com.uhg.optum.financials.shared.client.constant.PaymentType.PAYMENT;

@ExtendWith(MockitoExtension.class)
public class PaymentRequestDtoUtilsTests {

	public PaymentDetailsDto getPaymentDetailsDto() {
		PaymentDetailsDto paymentDetailsDto = new PaymentDetailsDto();
		paymentDetailsDto.setId("123");
		paymentDetailsDto.setReferenceId("123");
		String date = String.valueOf(LocalDate.of(2021, 9, 1));
		paymentDetailsDto.setPaymentDate(date);
		paymentDetailsDto.setAdjudicationCutoffDate(date);
		paymentDetailsDto.setAdjudicationVersion("1.0");
		paymentDetailsDto.setAmount(BigDecimal.valueOf(100.00));
		paymentDetailsDto.setRemitMethod(RemitMethod.PDF.name());
		paymentDetailsDto.setJurisdictionalBody("US");
		paymentDetailsDto.setNumberOfAttachedPdfs(1);
		paymentDetailsDto.setType(PAYMENT);
		return paymentDetailsDto;
	}

	public PayeeDetailsDto getPayeeDetailsDto() {
		PayeeDetailsDto payeeDetailsDto = new PayeeDetailsDto();
		payeeDetailsDto.setType(PayeeType.B.name());
		payeeDetailsDto.setName("John Doe");
		payeeDetailsDto.setTaxId("123456789");
		payeeDetailsDto.setNpi("1234567890");
		payeeDetailsDto.setReceiverId("1234567890");
		payeeDetailsDto.setAddress(getAddressDto());
		return payeeDetailsDto;
	}

	public AddressDto getAddressDto() {
		AddressDto addressDto = new AddressDto();
		addressDto.setLine1("123 Main St");
		addressDto.setLine2("Apt 1");
		addressDto.setCity("Minneapolis");
		addressDto.setState("MN");
		addressDto.setPostalCode("55401");
		addressDto.setCountry("US");
		return addressDto;
	}

	public ShippingDetailsDto getShippingDetailsDto() {
		ShippingDetailsDto shippingDetailsDto = new ShippingDetailsDto();
		shippingDetailsDto.setName("John Doe");
		shippingDetailsDto.setAddress(getAddressDto());
		return shippingDetailsDto;
	}

	public ClaimsDto getClaimsDto() {
		ClaimsDto claimsDto = new ClaimsDto();
		claimsDto.setTotalCount(1);
		claimsDto.setUrl("http://localhost:8080/claims");
		return claimsDto;
	}

	public PaymentRequestDto getPaymentRequestDto() {
		PaymentRequestDto paymentRequestDto = new PaymentRequestDto();
		paymentRequestDto.setCorrelationId("test");
		paymentRequestDto.setOrgShortCode("testCode");
		paymentRequestDto.setAccountShortCode("testCode");
		paymentRequestDto.setSubAccountShortCode("testCode");
		paymentRequestDto.setPaymentDetails(getPaymentDetailsDto());
		paymentRequestDto.setPayeeDetails(getPayeeDetailsDto());
		paymentRequestDto.setShippingDetails(getShippingDetailsDto());
		paymentRequestDto.setClaims(getClaimsDto());
		List<DocumentDto> documentDtos = new ArrayList<>();
		DocumentDto documentDto1 = new DocumentDto();
		documentDto1.setUrl("http://host:port/document1");
		documentDto1.setDocType("pdf");
		DocumentDto documentDto2 = new DocumentDto();
		documentDto2.setUrl("http://host:port/document2");
		documentDto2.setDocType("pdf");
		documentDtos.add(documentDto1);
		documentDtos.add(documentDto2);
		paymentRequestDto.setDocuments(documentDtos);
		return paymentRequestDto;
	}

}
