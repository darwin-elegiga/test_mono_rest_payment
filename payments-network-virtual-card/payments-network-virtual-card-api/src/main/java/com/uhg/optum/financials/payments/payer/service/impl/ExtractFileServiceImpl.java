package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.FileProcessingStatusDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.FileProcessingStatus;
import com.uhg.optum.financials.payments.payer.repository.FileProcessingStatusRepository;
import com.uhg.optum.financials.payments.payer.service.ExtractFileService;
import com.uhg.optum.financials.payments.payer.util.ModelConversionMapper;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeParseException;
import java.util.List;

@Service
@Slf4j
public class ExtractFileServiceImpl implements ExtractFileService {

	private final FileProcessingStatusRepository fileProcessingStatusRepository;

	private final ModelConversionMapper modelConversionMapper;

	@Autowired
	public ExtractFileServiceImpl(FileProcessingStatusRepository fileProcessingStatusRepository,
			ModelConversionMapper modelConversionMapper) {
		this.fileProcessingStatusRepository = fileProcessingStatusRepository;
		this.modelConversionMapper = modelConversionMapper;
	}

	@Override
	public List<FileProcessingStatusDto> getFileProcessingStatusByDate(String date) {
		LocalDate localDate;
		try {
			localDate = LocalDate.parse(date, DateTimeFormatter.ISO_DATE);
		}
		catch (DateTimeParseException exception) {
			throw new BadRequestException("Invalid date format. Please use 'YYYY-MM-DD'.");
		}

		if (localDate.isAfter(LocalDate.now())) {
			throw new BadRequestException("Date cannot be in the future. Please provide a date on or before today.");
		}

		String safeDateForLog = localDate.toString();

		LocalDateTime startOfDay = localDate.atStartOfDay();
		LocalDateTime endOfDay = localDate.atTime(23, 59, 59, 999_999_999);

		log.info("Querying file processing status for date: {}, range: {} to {}", safeDateForLog, startOfDay, endOfDay);

		List<FileProcessingStatus> fileProcessingStatusList = this.fileProcessingStatusRepository
			.findAllByProcessedAtBetween(startOfDay, endOfDay);

		log.info("Found {} file(s) processed on date: {}", fileProcessingStatusList.size(), safeDateForLog);

		if (fileProcessingStatusList.isEmpty()) {
			log.warn("No files were processed on date: {}", safeDateForLog);
			throw new NotFoundException("No files were processed on date " + safeDateForLog + ".");
		}

		return fileProcessingStatusList.stream().map(this.modelConversionMapper::fileProcessingStatusDto).toList();
	}

}
