package com.uhg.optum.financials.payments.payer.job.mapper;

import com.uhg.optum.financials.shared.client.projection.PaymentSegment;
import org.springframework.jdbc.core.RowMapper;

import java.sql.ResultSet;
import java.sql.SQLException;

public class PaymentSegmentRowMapper implements RowMapper<PaymentSegment> {

	@Override
	public PaymentSegment mapRow(ResultSet rs, int rowNum) throws SQLException {
		PaymentSegmentDto paymentSegmentDto = new PaymentSegmentDto();
		paymentSegmentDto.setOrgShortCode(rs.getString("org_short_code"));
		paymentSegmentDto.setAccountShortCode(rs.getString("account_short_code"));
		paymentSegmentDto.setSubAccountShortCode(rs.getString("sub_account_short_code"));
		paymentSegmentDto.setPayer835Id(rs.getString("payer835_id"));
		paymentSegmentDto.setSubmitterId(rs.getString("submitter_id"));
		paymentSegmentDto.setPaymentId(rs.getString("payment_id"));
		paymentSegmentDto.setStSeSegment(rs.getString("st_se_segment"));
		paymentSegmentDto.setPaymentAmount(rs.getDouble("payment_amount"));
		paymentSegmentDto.setNetworkPaymentId(rs.getString("network_payment_id"));
		paymentSegmentDto.setPayerName(rs.getString("payer_name"));
		paymentSegmentDto.setPayerShortCode(rs.getString("payer_short_code"));
		paymentSegmentDto.setFileIdentifier(rs.getString("file_identifier"));
		return paymentSegmentDto;
	}

}
