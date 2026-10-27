package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PayeeDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.ShippingDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.ClaimsDto;
import com.uhg.optum.financials.shared.client.dto.payment.DocumentDto;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.assertj.core.api.Assertions;
import org.mockito.Mockito;
import java.util.List;
import java.util.Map;
import java.util.HashMap;
import java.util.ArrayList;

@DisplayName("PaymentRequest Entity Tests")
class PaymentRequestTests {

	@Test
	@DisplayName("should map all fields to PaymentRequestDto correctly in toDto()")
	void shouldMapAllFieldsToDto() {
		// Arrange
		PaymentRequest paymentRequest = new PaymentRequest();
		this.setupPaymentRequest(paymentRequest);
		PaymentDetails paymentDetails = Mockito.mock(PaymentDetails.class);
		PaymentDetailsDto paymentDetailsDto = Mockito.mock(PaymentDetailsDto.class);
		Mockito.when(paymentDetails.toDto()).thenReturn(paymentDetailsDto);
		paymentRequest.setPaymentDetails(paymentDetails);

		PayeeDetails payeeDetails = Mockito.mock(PayeeDetails.class);
		PayeeDetailsDto payeeDetailsDto = Mockito.mock(PayeeDetailsDto.class);
		Mockito.when(payeeDetails.toDto()).thenReturn(payeeDetailsDto);
		paymentRequest.setPayeeDetails(payeeDetails);

		ShippingDetails shippingDetails = Mockito.mock(ShippingDetails.class);
		ShippingDetailsDto shippingDetailsDto = Mockito.mock(ShippingDetailsDto.class);
		Mockito.when(shippingDetails.toDto()).thenReturn(shippingDetailsDto);
		paymentRequest.setShippingDetails(shippingDetails);

		// Act
		PaymentRequestDto dto = paymentRequest.toDto();

		// Assert
		Assertions.assertThat(dto.getOrigin()).isEqualTo(paymentRequest.getOrigin());
		Assertions.assertThat(dto.getOrgShortCode()).isEqualTo(paymentRequest.getOrgShortCode());
		Assertions.assertThat(dto.getAccountShortCode()).isEqualTo(paymentRequest.getAccountShortCode());
		Assertions.assertThat(dto.getSubAccountShortCode()).isEqualTo(paymentRequest.getSubAccountShortCode());
		Assertions.assertThat(dto.getCorrelationId()).isEqualTo(paymentRequest.getCorrelationId());
		Assertions.assertThat(dto.getPaymentDetails()).isEqualTo(paymentDetailsDto);
		Assertions.assertThat(dto.getPayeeDetails()).isEqualTo(payeeDetailsDto);
		Assertions.assertThat(dto.getShippingDetails()).isEqualTo(shippingDetailsDto);
		Assertions.assertThat(dto.getClaims()).isNotNull();
		Assertions.assertThat(dto.getDocuments()).isEqualTo(paymentRequest.getDocuments());
		Assertions.assertThat(dto.getMetadata()).isEqualTo(paymentRequest.getMetadata());
	}

	@Test
	@DisplayName("should set and get all fields from BasePaymentRequest and PaymentRequest")
	void shouldSetAndGetAllFields() {
		// Arrange
		PaymentRequest paymentRequest = new PaymentRequest();
		this.setupPaymentRequest(paymentRequest);
		paymentRequest.setPayr835Id("835id");
		paymentRequest.setSubmitterId("submitterId");
		paymentRequest.setStatus("INIT");
		paymentRequest.setStSeSegment("segmentData");

		// Assert
		Assertions.assertThat(paymentRequest.getOrigin()).isEqualTo("Issuing");
		Assertions.assertThat(paymentRequest.getOrgShortCode()).isEqualTo("UHG01");
		Assertions.assertThat(paymentRequest.getAccountShortCode()).isEqualTo("FIN123");
		Assertions.assertThat(paymentRequest.getSubAccountShortCode()).isEqualTo("SUB456");
		Assertions.assertThat(paymentRequest.getCorrelationId()).isEqualTo("12345678-1234-1234-1234-123456789012");
		Assertions.assertThat(paymentRequest.getPayr835Id()).isEqualTo("835id");
		Assertions.assertThat(paymentRequest.getSubmitterId()).isEqualTo("submitterId");
		Assertions.assertThat(paymentRequest.getStatus()).isEqualTo("INIT");
		Assertions.assertThat(paymentRequest.getStSeSegment()).isEqualTo("segmentData");
	}

	@Test
	@DisplayName("should construct PaymentRequestDto with all fields")
	void shouldConstructDtoWithAllFields() {
		// Arrange
		PaymentDetailsDto paymentDetailsDto = Mockito.mock(PaymentDetailsDto.class);
		PayeeDetailsDto payeeDetailsDto = Mockito.mock(PayeeDetailsDto.class);
		ShippingDetailsDto shippingDetailsDto = Mockito.mock(ShippingDetailsDto.class);
		ClaimsDto claimsDto = Mockito.mock(ClaimsDto.class);
		List<DocumentDto> documents = new ArrayList<>();
		Map<String, String> metadata = new HashMap<>();
		metadata.put("orgShortCode", "OSFD5");
		metadata.put("accountShortCode", "1134");
		metadata.put("subAccountShortCode", "6656");

		PaymentRequestDto dto = new PaymentRequestDto("Issuing", "12345678-1234-1234-1234-123456789012", false, "UHG01",
				"FIN123", "SUB456", paymentDetailsDto, payeeDetailsDto, shippingDetailsDto, claimsDto, documents,
				metadata);

		// Assert
		Assertions.assertThat(dto.getOrigin()).isEqualTo("Issuing");
		Assertions.assertThat(dto.getCorrelationId()).isEqualTo("12345678-1234-1234-1234-123456789012");
		Assertions.assertThat(dto.getOrgShortCode()).isEqualTo("UHG01");
		Assertions.assertThat(dto.getAccountShortCode()).isEqualTo("FIN123");
		Assertions.assertThat(dto.getSubAccountShortCode()).isEqualTo("SUB456");
		Assertions.assertThat(dto.getPaymentDetails()).isEqualTo(paymentDetailsDto);
		Assertions.assertThat(dto.getPayeeDetails()).isEqualTo(payeeDetailsDto);
		Assertions.assertThat(dto.getShippingDetails()).isEqualTo(shippingDetailsDto);
		Assertions.assertThat(dto.getClaims()).isEqualTo(claimsDto);
		Assertions.assertThat(dto.getDocuments()).isEqualTo(documents);
		Assertions.assertThat(dto.getMetadata()).isEqualTo(metadata);
	}

	@Test
	@DisplayName("should construct empty PaymentRequestDto")
	void shouldConstructEmptyDto() {
		// Act
		PaymentRequestDto dto = new PaymentRequestDto();

		// Assert
		Assertions.assertThat(dto.getOrigin()).isNull();
		Assertions.assertThat(dto.getCorrelationId()).isNull();
		Assertions.assertThat(dto.getOrgShortCode()).isNull();
		Assertions.assertThat(dto.getAccountShortCode()).isNull();
		Assertions.assertThat(dto.getSubAccountShortCode()).isNull();
		Assertions.assertThat(dto.getPaymentDetails()).isNull();
		Assertions.assertThat(dto.getPayeeDetails()).isNull();
		Assertions.assertThat(dto.getShippingDetails()).isNull();
		Assertions.assertThat(dto.getClaims()).isNull();
		Assertions.assertThat(dto.getDocuments()).isNull();
		Assertions.assertThat(dto.getMetadata()).isNull();
	}

	private void setupPaymentRequest(PaymentRequest paymentRequest) {
		paymentRequest.setOrigin("Issuing");
		paymentRequest.setOrgShortCode("UHG01");
		paymentRequest.setAccountShortCode("FIN123");
		paymentRequest.setSubAccountShortCode("SUB456");
		paymentRequest.setCorrelationId("12345678-1234-1234-1234-123456789012");
		paymentRequest.setTotalClmCount(10);
		paymentRequest.setClmUrl("http://claims.url");
		paymentRequest.setDocType("PDF");
		paymentRequest.setPlb(true);
		List<DocumentDto> documents = new ArrayList<>();
		paymentRequest.setDocuments(documents);
		Map<String, String> metadata = new HashMap<>();
		metadata.put("orgShortCode", "OSFD5");
		metadata.put("accountShortCode", "1134");
		metadata.put("subAccountShortCode", "6656");
		paymentRequest.setMetadata(metadata);
	}

}
