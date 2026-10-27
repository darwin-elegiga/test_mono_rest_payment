package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.dto.PostedFileDto;
import com.uhg.optum.financials.payments.payer.dto.reports.AuthorizedFileReportDto;
import com.uhg.optum.financials.payments.payer.dto.reports.VCCNonFinancialReportDto;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;

import java.util.List;
import java.util.UUID;

public interface VCCReportsRepository extends JpaRepository<VirtualCard, UUID> {

	@Query(value = "SELECT " + "SUBSTRING(vc.masked_pan, 1, 6) || 'XXXXXX' || RIGHT(vc.masked_pan, 4) AS maskedPan, "
			+ "TO_CHAR(vc.created_at, 'MMDDYYYY') AS createdDate, "
			+ "TO_CHAR(vc.expiry_date, 'MMDDYYYY') AS expiryDate, "
			+ "CASE WHEN vc.status = 'CREATED' THEN 'PYNNN' WHEN vc.status = 'CANCELLED' THEN 'PNNNN' ELSE '' END AS status, "
			+ "vc.current_balance AS currentBalance, " + "vc.available_balance AS availableBalance, "
			+ "pd.issuer_payment_id AS issuerPaymentId, "
			+ "CONCAT_WS('/', pr.org_short_code, pr.account_short_code, pr.sub_account_short_code) AS orgAccountSubaccount "
			+ "FROM virtual_card.payment_request pr "
			+ "JOIN virtual_card.payment_details pd ON pd.id = pr.payment_details_id "
			+ "JOIN virtual_card.virtual_cards vc ON vc.payment_id = pr.payment_details_id "
			+ "WHERE (DATE(vc.created_at) = CURRENT_DATE - INTERVAL '1 day' "
			+ "OR DATE(vc.updated_at) = CURRENT_DATE - INTERVAL '1 day')", nativeQuery = true)
	List<VCCNonFinancialReportDto> findReportRowsByPaymentDetailsId();

	@Query(value = """
			SELECT
			  ce.masked_pan_number AS cardNumber,
			  TO_CHAR(ce.clearing_date, 'MMDDYYYY') AS transactionDate,
			  CASE
			    WHEN ce.settlement_indicator = 'CREDIT' THEN '2200-2S-0000'
			    WHEN ce.settlement_indicator = 'DEBIT'  THEN '2200-1S-0000'
			    ELSE ce.settlement_indicator
			  END AS transactionCode,
			  ce.clearing_amount AS transactionAmount,
			  CASE
			    WHEN ce.settlement_indicator = 'CREDIT' THEN '+'
			    WHEN ce.settlement_indicator = 'DEBIT'  THEN '-'
			    ELSE ce.settlement_indicator
			  END AS transactionAmountSign,
			  ce.approval_code AS authorizationCode,
			  TO_CHAR(ce.clearing_date, 'MMDDYYYY HH24:MI:SS') AS postDate,
			  CONCAT_WS('.', ce.merchant_id, ce.merchant_terminal_id) AS merchantNumber,
			  ce.merchant_name AS merchantName,
			  ce.merchant_category_code AS merchantCategoryCode,
			  pd.issuer_payment_id AS seExternalIdNumber,
			  ce.merchant_country_code AS merchantCountryCode,
			  ce.bin AS bin,
			  CONCAT_WS('/', pr.org_short_code, pr.account_short_code, pr.sub_account_short_code) AS tpa
			FROM virtual_card.clear_extract ce
			JOIN virtual_card.virtual_cards vc ON vc.card_guid = ce.card_id
			JOIN virtual_card.payment_request pr ON pr.payment_details_id = vc.payment_id
			JOIN virtual_card.payment_details pd ON pd.id = pr.payment_details_id
			WHERE ce.created_at::date = CURRENT_DATE - INTERVAL '1 day'
			""", nativeQuery = true)
	List<PostedFileDto> findPostedFileReportDetails();

	@Query(value = """
			SELECT
			SUBSTRING(ae.masked_card_number, 1, 6) || 'XXXXXX' || RIGHT(ae.masked_card_number, 4) AS cardNumber,
			TO_CHAR(ae.system_auth_time, 'MMDDYYYY HH24:MI:SS') AS transactionDate,
			CASE
			WHEN ae.auth_currency = 'USD' THEN '840'
			ELSE ae.auth_currency
			END AS TransactionCurrencyCode,
			NULL as addressVerificationResponse,
			ae.auth_action_code as authorizationResponse,
			ae.auth_amount  AS authorizationAmount,
			ae.approval_code AS authorizationCode,
			'MC' as networkCode,
			CONCAT_WS('.', ae.merchant_id, ae.merchant_terminal_id) AS merchantNumber,
			ae.merchant_name AS merchantName,
			ae.merchant_category_code AS merchantCategoryCode,
			pd.issuer_payment_id AS seExternalIdNumber,
			ae.merchant_country  AS merchantCountryCode,
			ae.bin AS bin,
			CONCAT_WS('/', pr.org_short_code, pr.account_short_code, pr.sub_account_short_code) AS tpa
			FROM virtual_card.auth_extract ae
			JOIN virtual_card.virtual_cards vc ON vc.card_guid = ae.card_id
			JOIN virtual_card.payment_request pr ON pr.payment_details_id = vc.payment_id
			JOIN virtual_card.payment_details pd ON pd.id = pr.payment_details_id
			WHERE DATE(ae.created_at) = CURRENT_DATE - INTERVAL '1 day'
			""", nativeQuery = true)
	List<AuthorizedFileReportDto> findAuthorizedFileReportDetails();

}
