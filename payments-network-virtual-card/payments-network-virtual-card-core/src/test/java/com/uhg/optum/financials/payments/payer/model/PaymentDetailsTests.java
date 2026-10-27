package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import com.uhg.optum.financials.shared.client.constant.PaymentType;
import com.uhg.optum.financials.shared.client.util.DateTimeUtil;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.assertj.core.api.Assertions;
import java.math.BigDecimal;
import java.time.LocalDate;

@DisplayName("PaymentDetails Entity Tests")
class PaymentDetailsTests {

	@Test
	@DisplayName("should map all fields to PaymentDetailsDto correctly in toDto()")
	void shouldMapAllFieldsToDto() {
		// Arrange
		PaymentDetails paymentDetails = new PaymentDetails();
		this.setupPaymentDetails(paymentDetails);
		paymentDetails.setNetworkPaymentId("networkId123");

		// Act
		PaymentDetailsDto dto = paymentDetails.toDto();

		// Assert
		Assertions.assertThat(dto.getId()).isEqualTo(paymentDetails.getId());
		Assertions.assertThat(dto.getIssuerPaymentId()).isEqualTo(paymentDetails.getIssuerPaymentId());
		Assertions.assertThat(dto.getReferenceId()).isEqualTo(paymentDetails.getReferenceId());
		Assertions.assertThat(dto.getPaymentDate())
			.isEqualTo(DateTimeUtil.toYYYYMMDDDateWithHyphen(paymentDetails.getPaymentDate()));
		Assertions.assertThat(dto.getAdjudicationCutoffDate())
			.isEqualTo(DateTimeUtil.toYYYYMMDDDateWithHyphen(paymentDetails.getAdjudicationCutoffDate()));
		Assertions.assertThat(dto.getAdjudicationVersion()).isEqualTo(paymentDetails.getAdjudicationVersion());
		Assertions.assertThat(dto.getAmount()).isEqualTo(paymentDetails.getAmount());
		Assertions.assertThat(dto.getRemitMethod()).isEqualTo(paymentDetails.getRemitMethod());
		Assertions.assertThat(dto.getJurisdictionalBody()).isEqualTo(paymentDetails.getJurisdictionalBody());
		Assertions.assertThat(dto.getNumberOfAttachedPdfs()).isEqualTo(paymentDetails.getNumberOfAttachedPdfs());
		Assertions.assertThat(dto.getSettlementTime()).isEqualTo(paymentDetails.getSettlementTime());
	}

	@Test
	@DisplayName("should set and get all fields from BasePaymentDetails and PaymentDetails")
	void shouldSetAndGetAllFields() {
		// Arrange
		PaymentDetails paymentDetails = new PaymentDetails();
		this.setupPaymentDetails(paymentDetails);
		paymentDetails.setNetworkPaymentId("networkId123");

		// Assert
		Assertions.assertThat(paymentDetails.getId()).isEqualTo("id123");
		Assertions.assertThat(paymentDetails.getIssuerPaymentId()).isEqualTo("issuerId456");
		Assertions.assertThat(paymentDetails.getReferenceId()).isEqualTo("ref789");
		Assertions.assertThat(paymentDetails.getPaymentDate()).isEqualTo(LocalDate.of(2025, 11, 30));
		Assertions.assertThat(paymentDetails.getAdjudicationCutoffDate()).isEqualTo(LocalDate.of(2025, 12, 1));
		Assertions.assertThat(paymentDetails.getAdjudicationVersion()).isEqualTo("v1");
		Assertions.assertThat(paymentDetails.getAmount()).isEqualByComparingTo(BigDecimal.valueOf(12345.67));
		Assertions.assertThat(paymentDetails.getRemitMethod()).isEqualTo("PDF");
		Assertions.assertThat(paymentDetails.getJurisdictionalBody()).isEqualTo("State Department of Health");
		Assertions.assertThat(paymentDetails.getNumberOfAttachedPdfs()).isEqualTo(5);
		Assertions.assertThat(paymentDetails.getSettlementTime()).isEqualTo("120000");
		Assertions.assertThat(paymentDetails.getType()).isEqualTo(PaymentType.PAYMENT);
		Assertions.assertThat(paymentDetails.getNetworkPaymentId()).isEqualTo("networkId123");
	}

	@Test
	@DisplayName("should construct PaymentDetailsDto with all fields")
	void shouldConstructDtoWithAllFields() {
		// Arrange
		BigDecimal amount = BigDecimal.valueOf(12345.67);
		PaymentDetailsDto dto = new PaymentDetailsDto("id123", "issuerId456", "ref789", "2025-11-30", "2025-12-01",
				"v1", amount, "PDF", "State Department of Health", 5, "120000", PaymentType.PAYMENT, "TEST_PIN_123");

		// Assert
		Assertions.assertThat(dto.getId()).isEqualTo("id123");
		Assertions.assertThat(dto.getIssuerPaymentId()).isEqualTo("issuerId456");
		Assertions.assertThat(dto.getReferenceId()).isEqualTo("ref789");
		Assertions.assertThat(dto.getPaymentDate()).isEqualTo("2025-11-30");
		Assertions.assertThat(dto.getAdjudicationCutoffDate()).isEqualTo("2025-12-01");
		Assertions.assertThat(dto.getAdjudicationVersion()).isEqualTo("v1");
		Assertions.assertThat(dto.getAmount()).isEqualByComparingTo(amount);
		Assertions.assertThat(dto.getRemitMethod()).isEqualTo("PDF");
		Assertions.assertThat(dto.getJurisdictionalBody()).isEqualTo("State Department of Health");
		Assertions.assertThat(dto.getNumberOfAttachedPdfs()).isEqualTo(5);
		Assertions.assertThat(dto.getSettlementTime()).isEqualTo("120000");
		Assertions.assertThat(dto.getType()).isEqualTo(PaymentType.PAYMENT);
		Assertions.assertThat(dto.getPin()).isEqualTo("TEST_PIN_123");
	}

	@Test
	@DisplayName("should construct empty PaymentDetailsDto")
	void shouldConstructEmptyDto() {
		// Act
		PaymentDetailsDto dto = new PaymentDetailsDto();

		// Assert
		Assertions.assertThat(dto.getId()).isNull();
		Assertions.assertThat(dto.getIssuerPaymentId()).isNull();
		Assertions.assertThat(dto.getReferenceId()).isNull();
		Assertions.assertThat(dto.getPaymentDate()).isNull();
		Assertions.assertThat(dto.getAdjudicationCutoffDate()).isNull();
		Assertions.assertThat(dto.getAdjudicationVersion()).isNull();
		Assertions.assertThat(dto.getAmount()).isNull();
		Assertions.assertThat(dto.getRemitMethod()).isNull();
		Assertions.assertThat(dto.getJurisdictionalBody()).isNull();
		Assertions.assertThat(dto.getNumberOfAttachedPdfs()).isEqualTo(0);
		Assertions.assertThat(dto.getSettlementTime()).isNull();
		Assertions.assertThat(dto.getType()).isNull();
	}

	private void setupPaymentDetails(PaymentDetails paymentDetails) {
		paymentDetails.setId("id123");
		paymentDetails.setIssuerPaymentId("issuerId456");
		paymentDetails.setReferenceId("ref789");
		paymentDetails.setPaymentDate(LocalDate.of(2025, 11, 30));
		paymentDetails.setAdjudicationCutoffDate(LocalDate.of(2025, 12, 1));
		paymentDetails.setAdjudicationVersion("v1");
		paymentDetails.setAmount(BigDecimal.valueOf(12345.67));
		paymentDetails.setRemitMethod("PDF");
		paymentDetails.setJurisdictionalBody("State Department of Health");
		paymentDetails.setNumberOfAttachedPdfs(5);
		paymentDetails.setSettlementTime("120000");
		paymentDetails.setType(PaymentType.PAYMENT);
	}

}
