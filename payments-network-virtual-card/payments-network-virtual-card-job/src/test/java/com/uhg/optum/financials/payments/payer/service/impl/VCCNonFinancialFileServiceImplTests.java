package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobContainerClient;
import com.uhg.optum.financials.payments.payer.dto.reports.VCCNonFinancialReportDto;
import com.uhg.optum.financials.payments.payer.model.BillingAddress;
import com.uhg.optum.financials.payments.payer.model.CardHolder;
import com.uhg.optum.financials.payments.payer.repository.CardholderRepository;
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
class VCCNonFinancialFileServiceImplTests {

	@TempDir
	private Path tempDir;

	@Mock
	private VCCReportsRepository vccReportsRepository;

	@Mock
	private CardholderRepository cardholderRepository;

	@Mock
	private FileTransferUtil fileTransferUtil;

	@Mock
	private ECGTransferUtil ecgTransferUtil;

	@Mock
	private BlobContainerClient blobContainerClient;

	@InjectMocks
	private VCCNonFinancialFileServiceImpl vccNonFinancialFileService;

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
		ReflectionTestUtils.setField(this.vccNonFinancialFileService, "nonFinancialReportPath", "/test/path/");
		ReflectionTestUtils.setField(this.vccNonFinancialFileService, "workDir", workDir);
	}

	@Test
	@DisplayName("givenValidData_whenCreateFile_thenFileIsCreatedAndUploadedAndTransferredAndCleanedUp")
	void givenValidData_whenCreateFile_thenFileIsCreatedAndUploadedAndTransferredAndCleanedUp() throws IOException {
		// given
		VCCNonFinancialReportDto dto = new VCCNonFinancialReportDto();
		dto.setMaskedPan("123456******7890");
		dto.setCreatedDate("20260127");
		dto.setExpiryDate("20270127");
		dto.setStatus("ACTIVE");
		dto.setCurrentBalance(BigDecimal.valueOf(1000.0));
		dto.setAvailableBalance(BigDecimal.valueOf(900.0));
		dto.setIssuerPaymentId("IPID123");
		dto.setOrgAccountSubaccount("ORGACC");
		CardHolder cardHolder = new CardHolder();
		cardHolder.setFirstName("John");
		cardHolder.setLastName("Doe");
		cardHolder.setPhoneNumber("123-456-7890");
		BillingAddress billingAddress = new BillingAddress();
		billingAddress.setLine1("123 Main St");
		billingAddress.setCity("Metropolis");
		billingAddress.setPostalCode("12345");
		billingAddress.setCountry("US");
		cardHolder.setBillingAddress(billingAddress);
		given(this.vccReportsRepository.findReportRowsByPaymentDetailsId()).willReturn(Collections.singletonList(dto));
		given(this.cardholderRepository.findCardholder()).willReturn(cardHolder);

		// when
		String fileName = this.vccNonFinancialFileService.createFile();

		// then
		assertThat(fileName).endsWith("_OptumChoice_nonfinancial_op_debit.TXT");
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
		given(this.vccReportsRepository.findReportRowsByPaymentDetailsId()).willThrow(new RuntimeException("DB error"));

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
