package com.uhg.optum.financials.payments.payer.job.mapper;

import com.uhg.optum.financials.payments.payer.dto.CompanionDto;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import java.sql.ResultSet;
import java.sql.SQLException;

import static com.uhg.optum.financials.payments.payer.constant.JobConstant.CDEVC_URL;

@ExtendWith(MockitoExtension.class)
public class CompanionDataRowMapperTests {

	@Test
	public void testMapRow() throws SQLException {
		String baseUrl = "https://example.com";
		CompanionDataRowMapper companionDataRowMapper = new CompanionDataRowMapper(baseUrl);
		ResultSet resultSet = org.mockito.Mockito.mock(ResultSet.class);
		Mockito.when(resultSet.getString("payment_type")).thenReturn("type1");
		Mockito.when(resultSet.getString("network_payment_id")).thenReturn("network123");
		Mockito.when(resultSet.getString("sub_account_short_cd")).thenReturn("SUB12345");
		Mockito.when(resultSet.getString("account_short_cd")).thenReturn("SC001");
		Mockito.when(resultSet.getString("summary_page_url")).thenReturn("http://example.com/summary");
		Mockito.when(resultSet.getString("issuer_payment_ids")).thenReturn("issuer1,issuer2");
		Mockito.when(resultSet.getString("payment_ids")).thenReturn("payment1,payment2");
		CompanionDto companionDto = companionDataRowMapper.mapRow(resultSet, 1);

		Assertions.assertThat(companionDto).isNotNull();
		Assertions.assertThat(companionDto.getPaymentType()).isEqualTo("type1");
		Assertions.assertThat(companionDto.getNetworkPaymentID()).isEqualTo("network123");
		Assertions.assertThat(companionDto.getSubAccountShortCode()).isEqualTo("SUB12345");
		Assertions.assertThat(companionDto.getAccountShortCode()).isEqualTo("SC001");
		Assertions.assertThat(companionDto.getSummaryPageURL()).isEqualTo("http://example.com/summary");
		Assertions.assertThat(companionDto.getSummaryPageUnmaskedURL()).isEqualTo(baseUrl + CDEVC_URL + "network123");
		Assertions.assertThat(companionDto.getIssuerPayments())
			.hasSize(2)
			.extracting(CompanionDto.IssuerPayment::getIssuerPaymentID, CompanionDto.IssuerPayment::getPaymentId)
			.containsExactly(Assertions.tuple("issuer1", "payment1"), Assertions.tuple("issuer2", "payment2"));

		Mockito.verify(resultSet).getString("payment_type");
		Mockito.verify(resultSet, Mockito.times(1)).getString("network_payment_id");
		Mockito.verify(resultSet).getString("sub_account_short_cd");
		Mockito.verify(resultSet).getString("account_short_cd");
		Mockito.verify(resultSet).getString("summary_page_url");
		Mockito.verify(resultSet).getString("issuer_payment_ids");
		Mockito.verify(resultSet).getString("payment_ids");

	}

}
