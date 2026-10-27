package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.dto.AuditLogDto;
import com.uhg.optum.financials.payments.payer.dto.FileProcessingStatusDto;
import com.uhg.optum.financials.payments.payer.dto.RuleDto;
import com.uhg.optum.financials.payments.payer.dto.VirtualCardHistoryDto;
import com.uhg.optum.financials.payments.payer.dto.authevent.Money;
import com.uhg.optum.financials.payments.payer.model.AuditLog;
import com.uhg.optum.financials.payments.payer.model.FileProcessingStatus;
import com.uhg.optum.financials.payments.payer.model.ManageSpendRules;
import com.uhg.optum.financials.payments.payer.model.VirtualCardHistory;
import org.modelmapper.Converter;
import org.modelmapper.ModelMapper;
import org.modelmapper.spi.MappingContext;
import org.springframework.stereotype.Component;

import java.math.BigDecimal;

@Component
public class ModelConversionMapper {

	private final ModelMapper modelMapper;

	public ModelConversionMapper(ModelMapper modelMapper) {
		this.modelMapper = modelMapper;
	}

	public <T, U> U modelMapper(T source, Class<U> destinationType) {
		this.modelMapper.addConverter(new Converter<Money, BigDecimal>() {
			public BigDecimal convert(MappingContext<Money, BigDecimal> context) {
				Money source = context.getSource();
				return (source != null) ? BigDecimal.valueOf(Long.parseLong(source.getValue())) : null;
			}
		});
		return this.modelMapper.map(source, destinationType);
	}

	public AuditLogDto toAuditLogDto(AuditLog auditLog) {
		return this.modelMapper.map(auditLog, AuditLogDto.class);
	}

	public FileProcessingStatusDto fileProcessingStatusDto(FileProcessingStatus fileProcessingStatus) {
		if (fileProcessingStatus == null) {
			return null;
		}
		return this.modelMapper.map(fileProcessingStatus, FileProcessingStatusDto.class);
	}

	public VirtualCardHistoryDto toVirtualCardHistoryDto(VirtualCardHistory virtualCardHistory) {
		return this.modelMapper.map(virtualCardHistory, VirtualCardHistoryDto.class);
	}

	public <T, U> U spendRuleModelMapper(T source, Class<U> destinationType) {
		this.modelMapper.typeMap(RuleDto.class, ManageSpendRules.class)
			.addMappings((mapper) -> mapper.skip(ManageSpendRules::setId));
		return this.modelMapper.map(source, destinationType);
	}

}
