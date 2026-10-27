package com.uhg.optum.financials.payments.payer.job.reader;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.jdbc.core.PreparedStatementSetter;
import org.springframework.test.util.ReflectionTestUtils;

import javax.sql.DataSource;

import java.sql.PreparedStatement;

import static com.uhg.optum.financials.payments.payer.constant.JobConstant.PARAM;
import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.mock;

@ExtendWith(MockitoExtension.class)
class SegmentReaderTests {

	@Mock
	private DataSource dataSource;

	@InjectMocks
	private SegmentReader segmentReader;

	@BeforeEach
	void setUp() {
		ReflectionTestUtils.setField(this.segmentReader, "chunkSize", 10);
		ReflectionTestUtils.setField(this.segmentReader, PARAM,
				"{\"correlationId\":\"12345\",\"old835FileDate\":\"null\",\"remit\":\"false\"}");
	}

	@Test
	void segmentReader_initializesCorrectly_forNonRemitFlow() {
		ReflectionTestUtils.setField(this.segmentReader, PARAM,
				"{\"correlationId\":\"12345\",\"old835FileDate\":\"null\",\"remit\":\"false\"}");
		this.segmentReader.init();

		assertThat(this.segmentReader.getDataSource()).isNotNull();
		assertThat(this.segmentReader.getSql()).contains("pr.status = ?");
	}

	@Test
	void segmentReader_initializesCorrectly_forRemitFlow() {
		ReflectionTestUtils.setField(this.segmentReader, PARAM,
				"{\"correlationId\":\"12345\",\"old835FileDate\":\"20250701\",\"remit\":\"false\"}");
		this.segmentReader.init();

		assertThat(this.segmentReader.getDataSource()).isNotNull();
	}

	@Test
	void segmentReader_preparedStatementSetter_setsParametersCorrectly_forNonRemitFlow() throws Exception {
		ReflectionTestUtils.setField(this.segmentReader, PARAM,
				"{\"correlationId\":\"12345\",\"old835FileDate\":\"20250701\"}");

		// Use reflection to access the private method
		var method = SegmentReader.class.getDeclaredMethod("buildPreparedStatementSetter");
		method.setAccessible(true);
		PreparedStatementSetter setter = (PreparedStatementSetter) method.invoke(this.segmentReader);

		var preparedStatement = mock(PreparedStatement.class);
		setter.setValues(preparedStatement);

		Mockito.verify(preparedStatement).setString(1, "PI");
		Mockito.verify(preparedStatement).setString(2, "12345");
	}

}
