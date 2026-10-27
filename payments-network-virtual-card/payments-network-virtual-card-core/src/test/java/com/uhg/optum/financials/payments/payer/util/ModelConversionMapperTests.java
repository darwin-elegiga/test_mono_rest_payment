package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.dto.AuditLogDto;
import com.uhg.optum.financials.payments.payer.dto.FileProcessingStatusDto;
import com.uhg.optum.financials.payments.payer.dto.RuleDto;
import com.uhg.optum.financials.payments.payer.dto.authevent.Money;
import com.uhg.optum.financials.payments.payer.model.AuditLog;
import com.uhg.optum.financials.payments.payer.model.FileProcessingStatus;
import com.uhg.optum.financials.payments.payer.model.ManageSpendRules;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.modelmapper.ModelMapper;

import java.math.BigDecimal;

import static org.assertj.core.api.Assertions.assertThat;

class ModelConversionMapperTests {

	private ModelMapper thisModelMapper;

	private ModelConversionMapper thisMapper;

	@BeforeEach
	void setUp() {
		this.thisModelMapper = new ModelMapper();
		this.thisMapper = new ModelConversionMapper(this.thisModelMapper);
	}

	@Test
	@DisplayName("modelMapper maps Money to BigDecimal using custom converter")
	void modelMapper_shouldMapMoneyToBigDecimal() {
		// given
		Money thisMoney = new Money();
		thisMoney.setValue("12345");
		// when
		BigDecimal thisResult = this.thisMapper.modelMapper(thisMoney, BigDecimal.class);
		// then
		assertThat(thisResult).isEqualTo(BigDecimal.valueOf(12345L));
	}

	@Test
	@DisplayName("toAuditLogDto maps AuditLog to AuditLogDto")
	void toAuditLogDto_shouldMapAuditLogToDto() {
		// given
		AuditLog thisAuditLog = new AuditLog();
		thisAuditLog.setId(1L);
		thisAuditLog.setPaymentId("paymentId");
		// when
		AuditLogDto thisDto = this.thisMapper.toAuditLogDto(thisAuditLog);
		// then
		assertThat(thisDto).isNotNull();
		assertThat(thisDto.getId()).isEqualTo(1L);
		assertThat(thisDto.getPaymentId()).isEqualTo("paymentId");
	}

	@Test
	@DisplayName("fileProcessingStatusDto maps FileProcessingStatus to FileProcessingStatusDto")
	void fileProcessingStatusDto_shouldMapFileProcessingStatusToDto() {
		// given
		FileProcessingStatus thisFileProcessingStatus = new FileProcessingStatus();
		thisFileProcessingStatus.setId(42L);
		thisFileProcessingStatus.setFileName("test.csv");
		// when
		FileProcessingStatusDto thisDto = this.thisMapper.fileProcessingStatusDto(thisFileProcessingStatus);
		// then
		assertThat(thisDto).isNotNull();
		// assertThat(thisDto.getId()).isEqualTo(42L); // Commented out as getId() does
		// not exist
		assertThat(thisDto.getFileName()).isEqualTo("test.csv");
	}

	@Test
	@DisplayName("fileProcessingStatusDto returns null when input is null")
	void fileProcessingStatusDto_shouldReturnNullWhenInputIsNull() {
		// when
		FileProcessingStatusDto thisDto = this.thisMapper.fileProcessingStatusDto(null);
		// then
		assertThat(thisDto).isNull();
	}

	@Test
	@DisplayName("modelMapper maps RuleDto to ManageSpendRules using generic method")
	void modelMapper_shouldMapDtoToSpendRulesDto() {
		// given
		RuleDto thisDto = new RuleDto();
		thisDto.setRuleId("1234567890123456");
		// when
		ManageSpendRules thisEntity = this.thisMapper.spendRuleModelMapper(thisDto, ManageSpendRules.class);
		// then
		assertThat(thisEntity).isNotNull();
		assertThat(thisEntity.getRuleId()).isEqualTo("1234567890123456");
	}

}
