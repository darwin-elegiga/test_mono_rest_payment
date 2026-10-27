package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobContainerClient;
import com.uhg.optum.financials.payments.payer.dto.reports.AuthorizedFileReportDto;
import com.uhg.optum.financials.payments.payer.repository.VCCReportsRepository;
import com.uhg.optum.financials.payments.payer.util.ECGTransferUtil;
import com.uhg.optum.financials.payments.payer.util.FileTransferUtil;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.junit.jupiter.api.io.TempDir;
import org.mockito.ArgumentCaptor;
import org.mockito.ArgumentMatchers;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.test.util.ReflectionTestUtils;

import java.io.File;
import java.io.IOException;
import java.math.BigDecimal;
import java.nio.file.Path;
import java.util.Collections;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.Mockito.times;

@ExtendWith(MockitoExtension.class)
class AuthorizedFileCreationServiceImplTests {

	@TempDir
	private Path tempDir;

	@Mock
	private VCCReportsRepository vccReportsRepository;

	@Mock
	private FileTransferUtil fileTransferUtil;

	@Mock
	private ECGTransferUtil ecgTransferUtil;

	@Mock
	private BlobContainerClient blobContainerClient;

	@InjectMocks
	private AuthorizedFileCreationServiceImpl vccNonFinancialFileService;

	@BeforeEach
	void setUp() {
		// Convert absolute path to root-relative Unix-style so FilenameUtils.getPath()
		// in the service does not strip a Windows drive letter and produces a valid path
		// on both Windows (root-relative) and Linux (already absolute).
		String path = this.tempDir.toAbsolutePath().toString().replace('\\', '/');
		if (path.length() > 1 && path.charAt(1) == ':') {
			path = path.substring(2); // strip "C:" → "/Users/..."
		}
		String workDir = path.endsWith("/") ? path : path + "/";
		ReflectionTestUtils.setField(this.vccNonFinancialFileService, "authorizedFileReportPath", "/test/path/");
		ReflectionTestUtils.setField(this.vccNonFinancialFileService, "workDir", workDir);
	}

	@Test
	@DisplayName("givenValidData_whenCreateFile_thenFileIsCreatedAndUploadedAndTransferredAndCleanedUp")
	void givenValidData_whenCreateFile_thenFileIsCreatedAndUploadedAndTransferredAndCleanedUp() throws IOException {
		// given
		AuthorizedFileReportDto dto = new AuthorizedFileReportDto();
		dto.setCardNumber("4111111111111111");
		dto.setBin("411111");
		dto.setAuthorizationAmount(BigDecimal.valueOf(100.00));
		dto.setAddressVerificationResponse("");
		dto.setAuthorizationCode("AUTH123");
		dto.setMerchantCategoryCode("5411");
		dto.setMerchantName("Test Merchant");
		dto.setTransactionDate("01012024");
		dto.setMerchantCountryCode("US");
		given(this.vccReportsRepository.findAuthorizedFileReportDetails()).willReturn(Collections.singletonList(dto));

		// when
		String fileName = this.vccNonFinancialFileService.createFile();

		// then
		assertThat(fileName).endsWith("_Optum_authorized_op_debit.TXT");
		ArgumentCaptor<File> uploadCaptor = ArgumentCaptor.forClass(File.class);
		then(this.fileTransferUtil).should(times(1))
			.uploadFile(ArgumentMatchers.eq(this.blobContainerClient), uploadCaptor.capture(),
					ArgumentMatchers.eq("/test/path/" + fileName));
		assertThat(uploadCaptor.getValue().getName()).isEqualTo(fileName);

		ArgumentCaptor<File> ecgCaptor = ArgumentCaptor.forClass(File.class);
		then(this.ecgTransferUtil).should(times(1)).transferToECG(ecgCaptor.capture());
		assertThat(ecgCaptor.getValue().getName()).isEqualTo(fileName);
	}

	@Test
	@DisplayName("givenRepositoryThrows_whenCreateFile_thenThrowsRuntimeException")
	void givenRepositoryThrows_whenCreateFile_thenThrowsRuntimeException() {
		// given
		given(this.vccReportsRepository.findAuthorizedFileReportDetails()).willThrow(new RuntimeException("DB error"));

		// when/then
		assertThatThrownBy(() -> this.vccNonFinancialFileService.createFile()).isInstanceOf(RuntimeException.class)
			.hasMessageContaining("DB error");
	}

	@Test
	@DisplayName("givenNullFields_whenFormatField_thenReturnsPaddedString")
	void givenNullFields_whenFormatField_thenReturnsPaddedString() {
		// given
		String result = ReflectionTestUtils.invokeMethod(this.vccNonFinancialFileService, "formatField", null, 5);
		// then
		assertThat(result).isEqualTo("");
	}

	@Test
	@DisplayName("givenLongString_whenFormatField_thenTruncates")
	void givenLongString_whenFormatField_thenTruncates() {
		// given
		String result = ReflectionTestUtils.invokeMethod(this.vccNonFinancialFileService, "formatField", "1234567890",
				5);
		// then
		assertThat(result).isEqualTo("12345");
	}

	@Test
	@DisplayName("givenShortString_whenFormatField_thenPads")
	void givenShortString_whenFormatField_thenPads() {
		// given
		String result = ReflectionTestUtils.invokeMethod(this.vccNonFinancialFileService, "formatField", "12", 5);
		// then
		assertThat(result).isEqualTo("12");
	}

}
