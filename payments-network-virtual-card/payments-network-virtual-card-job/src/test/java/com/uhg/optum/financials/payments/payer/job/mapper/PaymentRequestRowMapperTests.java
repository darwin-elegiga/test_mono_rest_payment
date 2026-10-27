package com.uhg.optum.financials.payments.payer.job.mapper;

import com.uhg.optum.financials.payments.payer.dto.PaymentRequestWrapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.Mockito;
import org.springframework.jdbc.core.RowMapper;

import java.sql.ResultSet;
import java.sql.SQLException;

import static org.assertj.core.api.Assertions.assertThat;

class PaymentRequestRowMapperTests {

	private RowMapper<PaymentRequestWrapper> rowMapper;

	private ResultSet resultSet;

	@BeforeEach
	void setUp() {
		this.rowMapper = new PaymentRequestRowMapper();
		this.resultSet = Mockito.mock(ResultSet.class);
	}

	@Test
	void testMapRow() throws SQLException {
		// Mocking the ResultSet
		Mockito.when(this.resultSet.getLong("id")).thenReturn(1L);
		Mockito.when(this.resultSet.getString("submitter_id")).thenReturn("submitter123");
		Mockito.when(this.resultSet.getString("status")).thenReturn("COMPLETED");
		Mockito.when(this.resultSet.getString("sub_account_short_code")).thenReturn("sub123");
		Mockito.when(this.resultSet.getString("account_short_code")).thenReturn("acc123");
		Mockito.when(this.resultSet.getString("org_short_code")).thenReturn("org123");
		Mockito.when(this.resultSet.getString("clm_url")).thenReturn("http://example.com");
		Mockito.when(this.resultSet.getString("metadata")).thenReturn("{\"key\":\"value\"}");
		Mockito.when(this.resultSet.getInt("total_clm_count")).thenReturn(10);
		Mockito.when(this.resultSet.getBoolean("plb")).thenReturn(true);
		Mockito.when(this.resultSet.getString("payment_details_id")).thenReturn("pd123");
		Mockito.when(this.resultSet.getDate("adjudication_cutoff_date")).thenReturn(null);
		Mockito.when(this.resultSet.getString("adjudication_version")).thenReturn("v1");
		Mockito.when(this.resultSet.getBigDecimal("amount")).thenReturn(new java.math.BigDecimal("100.00"));
		Mockito.when(this.resultSet.getString("issuer_payment_id")).thenReturn("issuer123");
		Mockito.when(this.resultSet.getString("reference_id")).thenReturn("ref123");
		Mockito.when(this.resultSet.getString("settlement_time")).thenReturn("2023-10-10T10:00:00Z");
		Mockito.when(this.resultSet.getLong("payee_details_id")).thenReturn(2L);
		Mockito.when(this.resultSet.getString("payee_name")).thenReturn("Payee Name");
		Mockito.when(this.resultSet.getString("npi")).thenReturn("npi123");
		Mockito.when(this.resultSet.getString("receiver_id")).thenReturn("receiver123");
		Mockito.when(this.resultSet.getString("tax_id")).thenReturn("tax123");
		Mockito.when(this.resultSet.getString("payee_address_line1")).thenReturn("123 Main St");
		Mockito.when(this.resultSet.getString("payee_address_line2")).thenReturn("Apt 4B");
		Mockito.when(this.resultSet.getString("payee_address_city")).thenReturn("City");
		Mockito.when(this.resultSet.getString("payee_address_state")).thenReturn("State");
		Mockito.when(this.resultSet.getString("payee_address_postal_code")).thenReturn("12345");
		Mockito.when(this.resultSet.getString("account_number")).thenReturn("123456789");
		Mockito.when(this.resultSet.getString("routing_number")).thenReturn("987654321");
		Mockito.when(this.resultSet.getString("sub_account_tin")).thenReturn("tin123");

		// Calling the mapRow method
		PaymentRequestWrapper result = this.rowMapper.mapRow(this.resultSet, 1);

		// Assertions
		assertThat(result).isNotNull();
		assertThat(result.getPaymentRequest().getId()).isEqualTo(1L);
		assertThat(result.getPaymentRequest().getSubmitterId()).isEqualTo("submitter123");
		assertThat(result.getPaymentRequest().getStatus()).isEqualTo("COMPLETED");
		assertThat(result.getPaymentRequest().getSubAccountShortCode()).isEqualTo("sub123");
		assertThat(result.getPaymentRequest().getTotalClmCount()).isEqualTo(10);
		assertThat(result.getPaymentRequest().isPlb()).isTrue();
		assertThat(result.getPaymentRequest().getPaymentDetails().getId()).isEqualTo("pd123");
		assertThat(result.getPaymentRequest().getPaymentDetails().getAdjudicationCutoffDate()).isNull();
		assertThat(result.getPaymentRequest().getPaymentDetails().getAdjudicationVersion()).isEqualTo("v1");
		assertThat(result.getPaymentRequest().getPaymentDetails().getAmount())
			.isEqualByComparingTo(new java.math.BigDecimal("100.00"));
		assertThat(result.getPaymentRequest().getPaymentDetails().getIssuerPaymentId()).isEqualTo("issuer123");
		assertThat(result.getPaymentRequest().getPaymentDetails().getReferenceId()).isEqualTo("ref123");
		assertThat(result.getPaymentRequest().getPaymentDetails().getSettlementTime())
			.isEqualTo("2023-10-10T10:00:00Z");
		assertThat(result.getPaymentRequest().getPayeeDetails().getId()).isEqualTo(2L);
		assertThat(result.getPaymentRequest().getPayeeDetails().getName()).isEqualTo("Payee Name");
		assertThat(result.getPaymentRequest().getPayeeDetails().getNpi()).isEqualTo("npi123");
		assertThat(result.getPaymentRequest().getPayeeDetails().getReceiverId()).isEqualTo("receiver123");
		assertThat(result.getPaymentRequest().getPayeeDetails().getTaxId()).isEqualTo("tax123");
		assertThat(result.getPaymentRequest().getPayeeDetails().getAddress().getPostalCode()).isEqualTo("12345");
		assertThat(result.getSubAccountBankAccountDetails().getBankAccountNumber()).isEqualTo("123456789");
		assertThat(result.getSubAccountBankAccountDetails().getBankRoutingNumber()).isEqualTo("987654321");
	}

}
