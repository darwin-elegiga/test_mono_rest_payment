package com.uhg.optum.financials.payments.payer.job;

import com.uhg.optum.financials.payments.payer.service.PostedFileCreationService;
import com.uhg.optum.financials.payments.payer.service.impl.VCCNonFinancialFileServiceImpl;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.io.IOException;

import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.Mockito.times;

@ExtendWith(MockitoExtension.class)
class VCCReportsJobTests {

	@Mock
	private VCCNonFinancialFileServiceImpl vccNonFinancialFile;

	@Mock
	private PostedFileCreationService postedFileCreationService;

	@InjectMocks
	private VCCReportsJob vccReportsJob;

	@BeforeEach
	void setUp() {
		// No-op, handled by @InjectMocks
	}

	@Test
	@DisplayName("generateVCCNonFinancialFile: should log and call createFile successfully (BDD style)")
	void givenVCCNonFinancialFileService_whenGenerateVCCNonFinancialFile_thenCreateFileIsCalled() throws IOException {
		// given
		String fileName = "nonFinancialFile.txt";
		given(this.vccNonFinancialFile.createFile()).willReturn(fileName);

		// when
		this.vccReportsJob.generateVCCNonFinancialFile();

		// then
		then(this.vccNonFinancialFile).should(times(1)).createFile();
	}

	@Test
	@DisplayName("generateVCCNonFinancialFile: should throw IOException when createFile fails (BDD style)")
	void givenVCCNonFinancialFileServiceThrows_whenGenerateVCCNonFinancialFile_thenIOExceptionIsThrown()
			throws IOException {
		// given
		given(this.vccNonFinancialFile.createFile()).willThrow(new IOException("IO error"));

		// when/then
		assertThatThrownBy(() -> this.vccReportsJob.generateVCCNonFinancialFile()).isInstanceOf(IOException.class)
			.hasMessageContaining("IO error");
		then(this.vccNonFinancialFile).should(times(1)).createFile();
	}

	@Test
	@DisplayName("generatePostedFile: should log and call createFile successfully (BDD style)")
	void givenPostedFileCreationService_whenGeneratePostedFile_thenCreateFileIsCalled() throws IOException {
		// given
		String fileName = "postedFile.txt";
		given(this.postedFileCreationService.createFile()).willReturn(fileName);

		// when
		this.vccReportsJob.generatePostedFile();

		// then
		then(this.postedFileCreationService).should(times(1)).createFile();
	}

	@Test
	@DisplayName("generatePostedFile: should throw IOException when createFile fails (BDD style)")
	void givenPostedFileCreationServiceThrows_whenGeneratePostedFile_thenIOExceptionIsThrown() throws IOException {
		// given
		given(this.postedFileCreationService.createFile()).willThrow(new IOException("IO error"));

		// when/then
		assertThatThrownBy(() -> this.vccReportsJob.generatePostedFile()).isInstanceOf(IOException.class)
			.hasMessageContaining("IO error");
		then(this.postedFileCreationService).should(times(1)).createFile();
	}

}
