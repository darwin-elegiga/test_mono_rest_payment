package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.FileProcessingStatusDto;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.service.ExtractFileService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import org.springframework.http.ResponseEntity;

import java.util.Collections;
import java.util.List;

import static org.assertj.core.api.BDDAssertions.then;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.times;

class ExtractFileProcessingStatusControllerTests {

	@Mock
	private ExtractFileService extractFileService;

	@InjectMocks
	private ExtractFileController extractFileController;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this); // For test context, this is acceptable
	}

	@Test
	@DisplayName("should return file processing status list for given date")
	void shouldReturnFileProcessingStatusListForGivenDate() {
		// given
		String date = "2025-12-16";
		FileProcessingStatusDto dto = new FileProcessingStatusDto();
		List<FileProcessingStatusDto> expectedList = Collections.singletonList(dto);
		given(this.extractFileService.getFileProcessingStatusByDate(date)).willReturn(expectedList);

		// when
		ResponseEntity<List<FileProcessingStatusDto>> response = this.extractFileController
			.getFileProcessingStatusByDate(date);

		// then
		then(response.getStatusCode().value()).isEqualTo(200);
		then(response.getBody()).isEqualTo(expectedList);
		org.mockito.BDDMockito.then(this.extractFileService).should(times(1)).getFileProcessingStatusByDate(date);
	}

	@Test
	@DisplayName("should throw NotFoundException if no files processed on given date")
	void shouldThrowNotFoundExceptionIfNoFilesProcessedOnGivenDate() {
		// given
		String date = "2025-12-16";
		given(this.extractFileService.getFileProcessingStatusByDate(date))
			.willThrow(new NotFoundException("No files were processed on date " + date + "."));

		// when
		Throwable thrown = org.assertj.core.api.Assertions
			.catchThrowable(() -> this.extractFileController.getFileProcessingStatusByDate(date));

		// then
		then(thrown).isInstanceOf(NotFoundException.class).hasMessage("No files were processed on date " + date + ".");
		org.mockito.BDDMockito.then(this.extractFileService).should(times(1)).getFileProcessingStatusByDate(date);
	}

}
