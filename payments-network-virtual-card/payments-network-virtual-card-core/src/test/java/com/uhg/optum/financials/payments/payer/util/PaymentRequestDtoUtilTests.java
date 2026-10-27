package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.model.PayeeDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.ShippingDetails;
import com.uhg.optum.financials.payments.payer.model.Address;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.client.dto.payment.DocumentDto;

import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.Collections;

@ExtendWith(MockitoExtension.class)
public class PaymentRequestDtoUtilTests {

	@InjectMocks
	PaymentRequestDtoUtil paymentRequestDtoUtil;

	private PaymentRequest paymentRequest = new PaymentRequest();

	@BeforeEach
	void setUp() {
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("PAY123");
		paymentDetails.setIssuerPaymentId("ISSUER123");
		paymentDetails.setReferenceId("REF123");
		paymentDetails.setPaymentDate(LocalDate.parse("2025-01-01"));
		paymentDetails.setAdjudicationCutoffDate(LocalDate.parse("2025-01-15"));
		paymentDetails.setAdjudicationVersion("v1");
		paymentDetails.setAmount(BigDecimal.valueOf(100.0));
		paymentDetails.setRemitMethod("EFT");
		paymentDetails.setJurisdictionalBody("Federal");
		paymentDetails.setNumberOfAttachedPdfs(2);
		paymentDetails.setSettlementTime("2025-01-20");
		Address address = new Address();
		address.setLine1("123 Main St");
		address.setLine2("Apt 4B");
		address.setCity("New York");
		address.setState("NY");
		address.setCountry("USA");
		address.setPostalCode("10001");
		PayeeDetails payeeDetails = new PayeeDetails();
		payeeDetails.setType("Individual");
		payeeDetails.setName("John Doe");
		payeeDetails.setAddress(address);
		payeeDetails.setTaxId("123-45-6789");
		payeeDetails.setNpi("9876543210");
		payeeDetails.setReceiverId("Receiver123");
		ShippingDetails shippingDetails = new ShippingDetails();
		shippingDetails.setName("Jane Doe");
		shippingDetails.setAddress(address);

		this.paymentRequest.setPaymentDetails(paymentDetails);
		this.paymentRequest.setPayeeDetails(payeeDetails);
		this.paymentRequest.setShippingDetails(shippingDetails);
	}

	@Test
	void testMapToDto() {
		this.paymentRequest.setCorrelationId("test-correlation-id");
		this.paymentRequest.setOrgShortCode("ORG123");
		this.paymentRequest.setAccountShortCode("ACC123");
		this.paymentRequest.setSubAccountShortCode("SUB123");
		this.paymentRequest.setDocuments(Collections.singletonList(new DocumentDto("PDF", "url")));
		this.paymentRequest.setTotalClmCount(5);
		this.paymentRequest.setClmUrl("claim-url");
		this.paymentRequest.setDocType("doc-type");
		this.paymentRequest.setPlb(true);

		PaymentRequestDto dto = this.paymentRequestDtoUtil.mapToDto(this.paymentRequest);

		Assertions.assertThat(dto);
		Assertions.assertThat(dto.getCorrelationId()).isNotNull();
		Assertions.assertThat(dto.getOrgShortCode()).isNotNull();
		Assertions.assertThat(dto.getAccountShortCode()).isNotNull();
		Assertions.assertThat(dto.getSubAccountShortCode()).isNotNull();
		Assertions.assertThat(dto.getPaymentDetails()).isNotNull();
		Assertions.assertThat(dto.getPayeeDetails()).isNotNull();
		Assertions.assertThat(dto.getShippingDetails()).isNotNull();
		Assertions.assertThat(dto.getDocuments()).isNotNull();
		Assertions.assertThat(dto.getClaims().getDocType()).isNotNull();
		Assertions.assertThat(dto.getClaims().isPlb());

	}

	@Test
	void testMapToDto_NullInput() {
		PaymentRequestDto dto = this.paymentRequestDtoUtil.mapToDto(null);
		Assertions.assertThat(dto).isNull();
	}

}
