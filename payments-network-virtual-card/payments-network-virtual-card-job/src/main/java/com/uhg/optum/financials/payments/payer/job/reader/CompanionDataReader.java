package com.uhg.optum.financials.payments.payer.job.reader;

import com.uhg.optum.financials.payments.payer.dto.CompanionDto;
import com.uhg.optum.financials.payments.payer.job.mapper.CompanionDataRowMapper;
import org.springframework.batch.core.configuration.annotation.JobScope;
import org.springframework.batch.infrastructure.item.database.JdbcCursorItemReader;
import org.springframework.beans.factory.annotation.Value;

import javax.sql.DataSource;

@JobScope
public class CompanionDataReader extends JdbcCursorItemReader<CompanionDto> {

	private final String correlationId;

	@Value("${com.optum.job.chunkSize}")
	private int chunkSize;

	private final String baseUrl;

	public CompanionDataReader(String correlationId, DataSource dataSource, String baseUrl) {
		super(dataSource, "", new CompanionDataRowMapper(baseUrl));

		if (dataSource == null) {
			throw new IllegalArgumentException("DataSource cannot be null");
		}
		this.correlationId = correlationId;
		this.baseUrl = baseUrl;

		this.setDataSource(dataSource);
		String sql = """
						SELECT
						      pd.network_payment_id,
					      pr.sub_account_short_code as sub_account_short_cd,
					      pr.account_short_code as account_short_cd,
						      drd.document_url as summary_page_url,
						      CASE
						          WHEN BOOL_OR(pd.type = 'Payment') THEN 'I'
						              WHEN BOOL_OR(pd.type = 'Notification') THEN 'H'
						              WHEN BOOL_OR(pd.type = 'PaymentOnly') AND SUM(pd.amount) > 0 THEN 'D'
						              ELSE 'H'
						      END as payment_type,
						      string_agg(pd.issuer_payment_id::text,',') as issuer_payment_ids,
						      string_agg(pd.id::text,',') as payment_ids
						FROM
						      virtual_card.payment_request pr
						join virtual_card.payment_details pd on
						      pr.payment_details_id = pd.id
						join virtual_card.document_response_details drd on
						      pd.network_payment_id = drd.network_payment_id
						WHERE
						      pr.correlation_id = ?
						group by
						      pd.network_payment_id,
									      pr.sub_account_short_code,
									      pr.account_short_code,
						      drd.document_url
				""";
		this.setSql(sql);
		this.setPreparedStatementSetter((ps) -> ps.setString(1, this.correlationId));
		this.setRowMapper(new CompanionDataRowMapper(this.baseUrl));
		this.setFetchSize(this.chunkSize);
	}

}
