package com.uhg.optum.financials.payments.payer.job.mapper;

import com.uhg.optum.financials.shared.client.projection.PaymentSegment;
import org.junit.jupiter.api.Test;
import org.mockito.Mockito;

import java.sql.ResultSet;
import java.sql.SQLException;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.AssertionsForClassTypes.assertThatThrownBy;

class PaymentSegmentRowMapperTests {

	@Test
	void paymentSegmentRowMapper_mapsRowCorrectly() throws SQLException {
		ResultSet rs = Mockito.mock(ResultSet.class);
		Mockito.when(rs.getString("org_short_code")).thenReturn("ORG1");
		Mockito.when(rs.getString("account_short_code")).thenReturn("ACC1");
		Mockito.when(rs.getString("sub_account_short_code")).thenReturn("SUB1");
		Mockito.when(rs.getString("payer835_id")).thenReturn("PAYER1");
		Mockito.when(rs.getString("submitter_id")).thenReturn("SUBMIT1");
		Mockito.when(rs.getString("payment_id")).thenReturn("PAY1");
		Mockito.when(rs.getString("st_se_segment")).thenReturn("STSE1");
		Mockito.when(rs.getDouble("payment_amount")).thenReturn(100.0);
		Mockito.when(rs.getString("payer_name")).thenReturn("Payer Name");
		Mockito.when(rs.getString("payer_short_code")).thenReturn("PAYER_SHORT");
		Mockito.when(rs.getString("file_identifier")).thenReturn("FILE1");

		PaymentSegmentRowMapper rowMapper = new PaymentSegmentRowMapper();
		PaymentSegment paymentSegment = rowMapper.mapRow(rs, 1);

		assert paymentSegment != null;
		assertThat(paymentSegment.getOrgShortCode()).isEqualTo("ORG1");
		assertThat(paymentSegment.getAccountShortCode()).isEqualTo("ACC1");
		assertThat(paymentSegment.getSubAccountShortCode()).isEqualTo("SUB1");
		assertThat(paymentSegment.getPayer835Id()).isEqualTo("PAYER1");
		assertThat(paymentSegment.getSubmitterId()).isEqualTo("SUBMIT1");
		assertThat(paymentSegment.getPaymentId()).isEqualTo("PAY1");
		assertThat(paymentSegment.getStSeSegment()).isEqualTo("STSE1");
		assertThat(paymentSegment.getPaymentAmount()).isEqualTo(100.0);
		assertThat(paymentSegment.getPayerName()).isEqualTo("Payer Name");
		assertThat(paymentSegment.getFileIdentifier()).isEqualTo("FILE1");
	}

	@Test
	void paymentSegmentRowMapper_handlesNullValues() throws SQLException {
		ResultSet rs = Mockito.mock(ResultSet.class);
		Mockito.when(rs.getString("org_short_code")).thenReturn(null);
		Mockito.when(rs.getString("account_short_code")).thenReturn(null);
		Mockito.when(rs.getString("sub_account_short_code")).thenReturn(null);
		Mockito.when(rs.getString("payer835_id")).thenReturn(null);
		Mockito.when(rs.getString("submitter_id")).thenReturn(null);
		Mockito.when(rs.getString("payment_id")).thenReturn(null);
		Mockito.when(rs.getString("st_se_segment")).thenReturn(null);
		Mockito.when(rs.getDouble("payment_amount")).thenReturn(0.0);
		Mockito.when(rs.getString("payer_name")).thenReturn(null);
		Mockito.when(rs.getString("payer_short_code")).thenReturn(null);
		Mockito.when(rs.getString("file_identifier")).thenReturn(null);

		PaymentSegmentRowMapper rowMapper = new PaymentSegmentRowMapper();
		PaymentSegment paymentSegment = rowMapper.mapRow(rs, 1);

		assert paymentSegment != null;
		assertThat(paymentSegment.getOrgShortCode()).isNull();
		assertThat(paymentSegment.getAccountShortCode()).isNull();
		assertThat(paymentSegment.getSubAccountShortCode()).isNull();
		assertThat(paymentSegment.getPayer835Id()).isNull();
		assertThat(paymentSegment.getSubmitterId()).isNull();
		assertThat(paymentSegment.getPaymentId()).isNull();
		assertThat(paymentSegment.getStSeSegment()).isNull();
		assertThat(paymentSegment.getPaymentAmount()).isEqualTo(0.0);
		assertThat(paymentSegment.getPayerName()).isNull();
		assertThat(paymentSegment.getFileIdentifier()).isNull();
	}

	@Test
	void paymentSegmentRowMapper_handlesSQLException() throws SQLException {
		ResultSet rs = Mockito.mock(ResultSet.class);
		Mockito.when(rs.getString(Mockito.anyString())).thenThrow(new SQLException("SQL error"));

		PaymentSegmentRowMapper rowMapper = new PaymentSegmentRowMapper();

		assertThatThrownBy(() -> rowMapper.mapRow(rs, 1)).isInstanceOf(SQLException.class)
			.hasMessageContaining("SQL error");
	}

}
