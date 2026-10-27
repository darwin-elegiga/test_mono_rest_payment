package com.uhg.optum.financials.payments.payer.job.mapper;

import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;

class PaymentSegmentDtoTests {

	@Test
	void paymentSegmentDto_initializesCorrectly() {
		PaymentSegmentDto dto = new PaymentSegmentDto("ORG1", "ACC1", "SUB1", "PAYER1", "SUBMIT1", "STSE1", 100.0,
				"12345", "Payer Name", "PAYER_SHORT", "FILE1", "PAY1");
		Assertions.assertThat(dto.getOrgShortCode()).isEqualTo("ORG1");
		Assertions.assertThat(dto.getAccountShortCode()).isEqualTo("ACC1");
		Assertions.assertThat(dto.getSubAccountShortCode()).isEqualTo("SUB1");
		Assertions.assertThat(dto.getPayer835Id()).isEqualTo("PAYER1");
		Assertions.assertThat(dto.getSubmitterId()).isEqualTo("SUBMIT1");
		Assertions.assertThat(dto.getStSeSegment()).isEqualTo("STSE1");
		Assertions.assertThat(dto.getPaymentAmount()).isEqualTo(100.0);
		Assertions.assertThat(dto.getPayerName()).isEqualTo("Payer Name");
		Assertions.assertThat(dto.getPayerShortCode()).isEqualTo("PAYER_SHORT");
		Assertions.assertThat(dto.getFileIdentifier()).isEqualTo("FILE1");
		Assertions.assertThat(dto.getPaymentId()).isEqualTo("PAY1");
	}

	@Test
	void paymentSegmentDto_handlesNullValues() {
		PaymentSegmentDto dto = new PaymentSegmentDto();
		Assertions.assertThat(dto.getOrgShortCode()).isNull();
		Assertions.assertThat(dto.getAccountShortCode()).isNull();
		Assertions.assertThat(dto.getSubAccountShortCode()).isNull();
		Assertions.assertThat(dto.getPayer835Id()).isNull();
		Assertions.assertThat(dto.getSubmitterId()).isNull();
		Assertions.assertThat(dto.getStSeSegment()).isNull();
		Assertions.assertThat(dto.getPaymentAmount()).isNull();
		Assertions.assertThat(dto.getPayerName()).isNull();
		Assertions.assertThat(dto.getPayerShortCode()).isNull();
		Assertions.assertThat(dto.getFileIdentifier()).isNull();
		Assertions.assertThat(dto.getPaymentId()).isNull();
	}

	@Test
	void paymentSegmentDto_setsAndGetsValues() {
		PaymentSegmentDto dto = new PaymentSegmentDto();
		dto.setOrgShortCode("ORG1");
		dto.setAccountShortCode("ACC1");
		dto.setSubAccountShortCode("SUB1");
		dto.setPayer835Id("PAYER1");
		dto.setSubmitterId("SUBMIT1");
		dto.setStSeSegment("STSE1");
		dto.setPaymentAmount(100.0);
		dto.setPayerName("Payer Name");
		dto.setPayerShortCode("PAYER_SHORT");
		dto.setFileIdentifier("FILE1");
		dto.setPaymentId("PAY1");

		Assertions.assertThat(dto.getOrgShortCode()).isEqualTo("ORG1");
		Assertions.assertThat(dto.getAccountShortCode()).isEqualTo("ACC1");
		Assertions.assertThat(dto.getSubAccountShortCode()).isEqualTo("SUB1");
		Assertions.assertThat(dto.getPayer835Id()).isEqualTo("PAYER1");
		Assertions.assertThat(dto.getSubmitterId()).isEqualTo("SUBMIT1");
		Assertions.assertThat(dto.getStSeSegment()).isEqualTo("STSE1");
		Assertions.assertThat(dto.getPaymentAmount()).isEqualTo(100.0);
		Assertions.assertThat(dto.getPayerName()).isEqualTo("Payer Name");
		Assertions.assertThat(dto.getPayerShortCode()).isEqualTo("PAYER_SHORT");
		Assertions.assertThat(dto.getFileIdentifier()).isEqualTo("FILE1");
		Assertions.assertThat(dto.getPaymentId()).isEqualTo("PAY1");
	}

}
