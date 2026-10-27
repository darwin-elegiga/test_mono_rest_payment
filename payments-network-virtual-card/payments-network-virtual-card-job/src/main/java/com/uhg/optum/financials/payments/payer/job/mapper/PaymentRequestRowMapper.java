package com.uhg.optum.financials.payments.payer.job.mapper;

import com.uhg.optum.financials.payments.payer.dto.PaymentRequestWrapper;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountBankAccountDetails;
import com.uhg.optum.financials.payments.payer.model.Address;
import com.uhg.optum.financials.payments.payer.model.PayeeDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.shared.client.mapper.MetadataMapper;
import org.springframework.jdbc.core.RowMapper;

import java.sql.ResultSet;
import java.sql.SQLException;

public class PaymentRequestRowMapper implements RowMapper<PaymentRequestWrapper> {

	@Override
	public PaymentRequestWrapper mapRow(ResultSet rs, int rowNum) throws SQLException {
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setId(rs.getLong("id"));
		paymentRequest.setSubmitterId(rs.getString("submitter_id"));
		paymentRequest.setStatus(rs.getString("status"));
		paymentRequest.setSubAccountShortCode(rs.getString("sub_account_short_code"));
		paymentRequest.setAccountShortCode(rs.getString("account_short_code"));
		paymentRequest.setOrgShortCode(rs.getString("org_short_code"));
		paymentRequest.setClmUrl(rs.getString("clm_url"));
		paymentRequest.setMetadata(new MetadataMapper().convertToEntityAttribute(rs.getString("metadata")));
		paymentRequest.setTotalClmCount(rs.getInt("total_clm_count"));
		paymentRequest.setPlb(rs.getBoolean("plb"));

		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId(rs.getString("payment_details_id"));
		if (rs.getDate("adjudication_cutoff_date") != null) {
			paymentDetails.setAdjudicationCutoffDate(rs.getDate("adjudication_cutoff_date").toLocalDate());
		}
		else {
			paymentDetails.setAdjudicationCutoffDate(null);
		}
		paymentDetails.setAdjudicationVersion(rs.getString("adjudication_version"));
		paymentDetails.setAmount(rs.getBigDecimal("amount"));
		paymentDetails.setIssuerPaymentId(rs.getString("issuer_payment_id"));
		paymentDetails.setReferenceId(rs.getString("reference_id"));
		paymentDetails.setSettlementTime(rs.getString("settlement_time"));
		paymentRequest.setPaymentDetails(paymentDetails);

		PayeeDetails payeeDetails = new PayeeDetails();
		payeeDetails.setId(rs.getLong("payee_details_id"));
		payeeDetails.setName(rs.getString("payee_name"));
		payeeDetails.setNpi(rs.getString("npi"));
		payeeDetails.setReceiverId(rs.getString("receiver_id"));
		payeeDetails.setTaxId(rs.getString("tax_id"));
		Address payeeAddress = new Address();
		payeeAddress.setLine1(rs.getString("payee_address_line1"));
		payeeAddress.setLine2(rs.getString("payee_address_line2"));
		payeeAddress.setCity(rs.getString("payee_address_city"));
		payeeAddress.setState(rs.getString("payee_address_state"));
		payeeAddress.setPostalCode(rs.getString("payee_address_postal_code"));
		payeeDetails.setAddress(payeeAddress);
		paymentRequest.setPayeeDetails(payeeDetails);

		SubAccountBankAccountDetails subAccountBankAccountDetails = new SubAccountBankAccountDetails();
		subAccountBankAccountDetails.setBankAccountNumber(rs.getString("account_number"));
		subAccountBankAccountDetails.setBankRoutingNumber(rs.getString("routing_number"));

		PaymentRequestWrapper paymentRequestWrapper = new PaymentRequestWrapper();
		paymentRequestWrapper.setPaymentRequest(paymentRequest);
		paymentRequestWrapper.setSubAccountBankAccountDetails(subAccountBankAccountDetails);

		return paymentRequestWrapper;
	}

}
