package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.FileProcessingStatusDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.FileProcessingStatus;
import com.uhg.optum.financials.payments.payer.repository.FileProcessingStatusRepository;
import com.uhg.optum.financials.payments.payer.util.ModelConversionMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.Collections;
import java.util.List;

import static org.assertj.core.api.BDDAssertions.then;
import static org.mockito.BDDMockito.given;

class ExtractFileServiceImplTests {

	@Mock
	private FileProcessingStatusRepository fileProcessingStatusRepository;

	@Mock
	private ModelConversionMapper modelConversionMapper;

	@InjectMocks
	private ExtractFileServiceImpl extractFileProcessingStatusService;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	@DisplayName("should return DTO list for valid date")
	void shouldReturnDtoListForValidDate() {
		// given
		String date = "2025-12-16";
		LocalDate localDate = LocalDate.parse(date);
		LocalDateTime startOfDay = localDate.atStartOfDay();
		LocalDateTime endOfDay = localDate.atTime(23, 59, 59, 999_999_999);
		FileProcessingStatus entity = new FileProcessingStatus();
		FileProcessingStatusDto dto = new FileProcessingStatusDto();
		List<FileProcessingStatus> entityList = Collections.singletonList(entity);
		List<FileProcessingStatusDto> dtoList = Collections.singletonList(dto);
		given(this.fileProcessingStatusRepository.findAllByProcessedAtBetween(startOfDay, endOfDay))
			.willReturn(entityList);
		given(this.modelConversionMapper.fileProcessingStatusDto(entity)).willReturn(dto);

		// when
		List<FileProcessingStatusDto> result = this.extractFileProcessingStatusService
			.getFileProcessingStatusByDate(date);

		// then
		then(result).isEqualTo(dtoList);
	}

	@Test
	@DisplayName("should throw NotFoundException if no records found")
	void shouldThrowNotFoundExceptionIfNoRecordsFound() {
		// given
		String date = "2025-12-16";
		LocalDate localDate = LocalDate.parse(date);
		LocalDateTime startOfDay = localDate.atStartOfDay();
		LocalDateTime endOfDay = localDate.atTime(23, 59, 59, 999_999_999);
		given(this.fileProcessingStatusRepository.findAllByProcessedAtBetween(startOfDay, endOfDay))
			.willReturn(Collections.emptyList());

		// when
		Throwable thrown = org.assertj.core.api.Assertions
			.catchThrowable(() -> this.extractFileProcessingStatusService.getFileProcessingStatusByDate(date));

		// then
		then(thrown).isInstanceOf(NotFoundException.class).hasMessage("No files were processed on date " + date + ".");
	}

	@Test
	@DisplayName("should throw BadRequestException for invalid date format")
	void shouldThrowBadRequestExceptionForInvalidDateFormat() {
		// given
		String invalidDate = "16-12-2025";

		// when
		Throwable thrown = org.assertj.core.api.Assertions
			.catchThrowable(() -> this.extractFileProcessingStatusService.getFileProcessingStatusByDate(invalidDate));

		// then
		then(thrown).isInstanceOf(BadRequestException.class).hasMessageContaining("Invalid date format");
	}

}
