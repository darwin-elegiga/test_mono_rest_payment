package com.uhg.optum.financials.payments.payer.job.mapper;

import com.uhg.optum.financials.payments.payer.dto.CompanionDto;
import org.springframework.jdbc.core.RowMapper;

import java.sql.ResultSet;
import java.sql.SQLException;

import static com.uhg.optum.financials.payments.payer.constant.JobConstant.CDEVC_URL;

public class CompanionDataRowMapper implements RowMapper<CompanionDto> {

	private final String baseUrl;

	public CompanionDataRowMapper(String baseUrl) {
		this.baseUrl = baseUrl;
	}

	@Override
	public CompanionDto mapRow(ResultSet rs, int rowNum) throws SQLException {
		CompanionDto companionDto = new CompanionDto();

		String networkPaymentId = rs.getString("network_payment_id");
		companionDto.setPaymentType(rs.getString("payment_type"));
		companionDto.setNetworkPaymentID(networkPaymentId);
		companionDto.setSubAccountShortCode(rs.getString("sub_account_short_cd"));
		companionDto.setAccountShortCode(rs.getString("account_short_cd"));
		companionDto.setSummaryPageURL(rs.getString("summary_page_url"));
		String issuerPaymentIds = rs.getString("issuer_payment_ids");
		String paymentIds = rs.getString("payment_ids");
		companionDto.setIssuerPayments(issuerPaymentIds, paymentIds);
		companionDto.setSummaryPageUnmaskedURL(this.baseUrl + CDEVC_URL + networkPaymentId);
		return companionDto;
	}

}
