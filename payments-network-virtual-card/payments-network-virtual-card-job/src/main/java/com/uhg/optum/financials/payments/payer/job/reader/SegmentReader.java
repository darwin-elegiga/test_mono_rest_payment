package com.uhg.optum.financials.payments.payer.job.reader;

import com.uhg.optum.financials.payments.payer.job.mapper.PaymentSegmentRowMapper;
import com.uhg.optum.financials.payments.payer.enums.Status;
import com.uhg.optum.financials.shared.client.projection.PaymentSegment;
import jakarta.annotation.PostConstruct;
import lombok.extern.slf4j.Slf4j;
import org.springframework.batch.core.configuration.annotation.JobScope;
import org.springframework.batch.infrastructure.item.database.JdbcCursorItemReader;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.jdbc.core.PreparedStatementSetter;
import org.springframework.stereotype.Component;

import javax.sql.DataSource;

import static com.uhg.optum.financials.payments.payer.constant.JobConstant.CORRELATION_ID;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.PARAM;
import static com.uhg.optum.financials.payments.payer.util.ParameterProvider.getValue;

@Slf4j
@Component
@JobScope
public class SegmentReader extends JdbcCursorItemReader<PaymentSegment> {

	@Value("${com.optum.job.chunkSize}")
	private int chunkSize;

	@Value("#{jobParameters['" + PARAM + "']}")
	private String jobParam;

	private final DataSource dataSource;

	public SegmentReader(DataSource dataSource) {
		super(dataSource, "", new PaymentSegmentRowMapper());
		this.dataSource = dataSource;
	}

	@PostConstruct
	public void init() {
		this.setDataSource(this.dataSource);
		this.setSql(buildSql());
		this.setPreparedStatementSetter(buildPreparedStatementSetter());
		this.setRowMapper(new PaymentSegmentRowMapper());
		this.setFetchSize(this.chunkSize);
	}

	private String buildSql() {
		return """
				SELECT
				    pr.org_short_code AS org_short_code,
				    pr.account_short_code AS account_short_code,
				    pr.sub_account_short_code AS sub_account_short_code,
				    pr.payr835_id AS payer835_id,
				    pr.submitter_id as submitter_id,
				    pr.id as payment_id,
				    pr.st_se_segment as st_se_segment,
				    pd.amount AS payment_amount,
				    pd.network_payment_id as network_payment_id,
				    a.name AS payer_name,
				    pr.account_short_code AS payer_short_code,
				    COALESCE(pgl.group_level,'ORG') AS file_identifier
				FROM
				    virtual_card.payment_request pr
				INNER JOIN virtual_card.payment_details pd ON
				    pr.payment_details_id = pd.id
				INNER JOIN virtual_card.organizations ord ON
				    ord.org_short_code = pr.org_short_code
				INNER JOIN virtual_card.account a ON
				    a.account_short_code = pr.account_short_code
				    AND a.org_short_code = pr.org_short_code
				LEFT JOIN virtual_card.payment_grouping_level pgl ON
				    pr.org_short_code = pgl.org_short_code
				WHERE
				    pr.%s = ?
				    AND pr.correlation_id = ?
				    AND pr.st_se_segment IS NOT NULL
				ORDER BY
				    pr.org_short_code ASC,
				    pr.account_short_code ASC,
				    pr.sub_account_short_code ASC,
				    pr.payr835_id ASC,
				    pr.submitter_id ASC
				""".formatted("status");
	}

	private PreparedStatementSetter buildPreparedStatementSetter() {
		return (ps) -> {
			ps.setString(1, Status.PI.name());
			ps.setString(2, getValue(this.jobParam, CORRELATION_ID));
		};
	}

}
