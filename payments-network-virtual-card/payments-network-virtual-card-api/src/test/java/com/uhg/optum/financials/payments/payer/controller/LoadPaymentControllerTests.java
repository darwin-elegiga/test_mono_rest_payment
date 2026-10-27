package com.uhg.optum.financials.payments.payer.controller;

import com.azure.storage.blob.implementation.models.BlobStorageError;
import com.azure.storage.blob.models.BlobStorageException;
import com.uhg.optum.financials.payments.payer.constant.FileType;
import com.uhg.optum.financials.payments.payer.dto.FileTrackingDto;
import com.uhg.optum.financials.payments.payer.dto.PaymentResponseDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.service.FileService;
import com.uhg.optum.financials.payments.payer.service.PaymentService;
import com.uhg.optum.financials.payments.payer.testUtils.PaymentRequestDtoUtilsTests;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.mock.web.MockHttpServletResponse;
import org.springframework.web.servlet.mvc.method.annotation.StreamingResponseBody;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.FILE_NOT_FOUND_LOG;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.FILE_PATH_REQUIRED_LOG;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.INVALID_FILE_PATH_LOG;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.mock;

@ExtendWith(MockitoExtension.class)
public class LoadPaymentControllerTests {

	@InjectMocks
	private LoadPaymentController loadPaymentController;

	@Mock
	private PaymentService paymentService;

	@Mock
	private AuditLogger auditLogger;

	@Mock
	LogUtility logUtility;

	@Mock
	private PaymentResponseDto paymentResponseDto;

	@Mock
	private FileService fileService;

	private final MockHttpServletResponse response = new MockHttpServletResponse();

	PaymentRequestDtoUtilsTests paymentRequestDtoUtilsTests = new PaymentRequestDtoUtilsTests();

	@Test
	void loadPayment() {
		Assertions
			.assertThat(this.loadPaymentController.loadPayment(this.paymentRequestDtoUtilsTests.getPaymentRequestDto())
				.getStatusCode()
				.is2xxSuccessful())
			.isTrue();
	}

	@Test
	void getPaymentRequestDetail() {
		String paymentID = "12345";
		Assertions
			.assertThat(this.loadPaymentController.getPaymentRequestDetail(paymentID).getStatusCode().is2xxSuccessful())
			.isTrue();
	}

	@Test
	void getPaymentRequestDetail_success() {
		String paymentID = "12345";
		given(this.paymentService.getPaymentRequestDetail(eq(paymentID))).willReturn(this.paymentResponseDto);
		ResponseEntity<PaymentResponseDto> response = this.loadPaymentController.getPaymentRequestDetail(paymentID);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(this.paymentResponseDto);
	}

	@Test
	void getPaymentRequestDetail_notFound() {
		String paymentID = "notfound";
		given(this.paymentService.getPaymentRequestDetail(eq(paymentID))).willReturn(null);
		ResponseEntity<PaymentResponseDto> response = this.loadPaymentController.getPaymentRequestDetail(paymentID);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isNull();
	}

	@Test
	void getPaymentRequestDetail_serviceThrowsException() {
		String paymentID = "error";
		given(this.paymentService.getPaymentRequestDetail(eq(paymentID)))
			.willThrow(new IllegalArgumentException("Invalid payment id"));
		Assertions.assertThatThrownBy(() -> this.loadPaymentController.getPaymentRequestDetail(paymentID))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("Invalid payment id");
	}

	@Test
	void getFileName_success() {
		String correlationId = "correlation123";
		FileTrackingDto fileTrackingDto = new FileTrackingDto();
		given(this.paymentService.getFileName(eq(correlationId))).willReturn(fileTrackingDto);
		ResponseEntity<FileTrackingDto> response = this.loadPaymentController.getFileName(correlationId);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(fileTrackingDto);
	}

	@Test
	void getFileName_notFound() {
		String correlationId = "notfound";
		given(this.paymentService.getFileName(eq(correlationId))).willReturn(null);
		ResponseEntity<FileTrackingDto> response = this.loadPaymentController.getFileName(correlationId);
		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isNull();
	}

	@Test
	void getFileName_serviceThrowsException() {
		String correlationId = "error";
		given(this.paymentService.getFileName(eq(correlationId)))
			.willThrow(new IllegalArgumentException("Invalid correlation id"));
		Assertions.assertThatThrownBy(() -> this.loadPaymentController.getFileName(correlationId))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("Invalid correlation id");
	}

	@Test
	void testDownloadBlobFileUsingCorrelation() {
		FileTrackingDto fileTrackingDto = new FileTrackingDto();
		fileTrackingDto.set_835("test_835_file.txt");
		fileTrackingDto.setCompanion("test_companion_file.txt");

		Mockito.when(this.paymentService.getFileName("testCorrelationId")).thenReturn(fileTrackingDto);

		StreamingResponseBody streamingResponseBody = (outputStream) -> outputStream.write("File content".getBytes());
		Mockito.when(this.fileService.downloadBlobFile("test_835_file.txt", this.response))
			.thenReturn(streamingResponseBody);

		ResponseEntity<StreamingResponseBody> responseEntity = this.loadPaymentController
			.downloadBlobFileUsingCorrelation("testCorrelationId", FileType.PAYMENT835, this.response);
		Assertions.assertThat(responseEntity.getBody()).isNotNull();

		Assertions.assertThat(responseEntity.getStatusCode().is2xxSuccessful()).isTrue();

		responseEntity = this.loadPaymentController.downloadBlobFileUsingCorrelation("testCorrelationId",
				FileType.COMPANION, this.response);
		Assertions.assertThat(responseEntity.getStatusCode().is2xxSuccessful()).isTrue();

	}

	@Test
	void testDownloadBlobFileUsingCorrelation_BlobNotFound() {
		FileTrackingDto fileTrackingDto = new FileTrackingDto();
		fileTrackingDto.set_835("test_835_file.txt");

		Mockito.when(this.paymentService.getFileName("testCorrelationId")).thenReturn(fileTrackingDto);

		BlobStorageException blobStorageException = mock(BlobStorageException.class);
		BlobStorageError blobStorageError = mock(BlobStorageError.class);
		Mockito.when(blobStorageException.getValue()).thenReturn(blobStorageError);
		Mockito.when(blobStorageError.getCode()).thenReturn("BlobNotFound");
		Mockito.when(this.fileService.downloadBlobFile("test_835_file.txt", this.response))
			.thenThrow(blobStorageException);

		Assertions
			.assertThatThrownBy(() -> this.loadPaymentController.downloadBlobFileUsingCorrelation("testCorrelationId",
					FileType.PAYMENT835, this.response))
			.isInstanceOf(NotFoundException.class)
			.hasMessage(FILE_NOT_FOUND_LOG + ": test_835_file.txt");
	}

	@Test
	void testDownloadBlobFileUsingCorrelation_Exception() {
		FileTrackingDto fileTrackingDto = new FileTrackingDto();
		fileTrackingDto.set_835("test_835_file.txt");

		Mockito.when(this.paymentService.getFileName("testCorrelationId")).thenReturn(fileTrackingDto);

		Mockito.when(this.fileService.downloadBlobFile("test_835_file.txt", this.response))
			.thenThrow(new RuntimeException("Download failed"));

		Assertions
			.assertThatThrownBy(() -> this.loadPaymentController.downloadBlobFileUsingCorrelation("testCorrelationId",
					FileType.PAYMENT835, this.response))
			.isInstanceOf(RuntimeException.class)
			.hasMessage("Download failed");
	}

	@Test
	void testDownloadBlobFileUsingCorrelation_NullLocation() {
		FileTrackingDto fileTrackingDto = new FileTrackingDto();
		fileTrackingDto.set_835(null);

		Mockito.when(this.paymentService.getFileName("testCorrelationId")).thenReturn(fileTrackingDto);

		Assertions
			.assertThatThrownBy(() -> this.loadPaymentController.downloadBlobFileUsingCorrelation("testCorrelationId",
					FileType.PAYMENT835, this.response))
			.isInstanceOf(NotFoundException.class)
			.hasMessage(FILE_NOT_FOUND_LOG);
	}

	@Test
	void downloadBlobFileUsingCorrelation_fileNotFound_returnsNotFound() {
		FileTrackingDto fileTrackingDto = new FileTrackingDto();
		fileTrackingDto.set_835("test_835_file.txt");

		Mockito.when(this.paymentService.getFileName("testCorrelationId")).thenReturn(fileTrackingDto);

		BlobStorageException blobStorageException = mock(BlobStorageException.class);
		BlobStorageError blobStorageError = mock(BlobStorageError.class);
		Mockito.when(blobStorageException.getValue()).thenReturn(blobStorageError);
		Mockito.when(blobStorageError.getCode()).thenReturn("BlobNotFound");
		Mockito.when(this.fileService.downloadBlobFile("test_835_file.txt", this.response))
			.thenThrow(blobStorageException);

		Assertions
			.assertThatThrownBy(() -> this.loadPaymentController.downloadBlobFileUsingCorrelation("testCorrelationId",
					FileType.PAYMENT835, this.response))
			.isInstanceOf(NotFoundException.class)
			.hasMessage(FILE_NOT_FOUND_LOG + ": test_835_file.txt");
	}

	@Test
	void downloadBlobFileUsingCorrelation_internalServerError_returnsServerError() {
		FileTrackingDto fileTrackingDto = new FileTrackingDto();
		fileTrackingDto.set_835("test_835_file.txt");
		Mockito.when(this.paymentService.getFileName("testCorrelationId")).thenReturn(fileTrackingDto);
		Mockito.when(this.fileService.downloadBlobFile("test_835_file.txt", this.response))
			.thenThrow(new RuntimeException("Unexpected error occurred"));
		Assertions
			.assertThatThrownBy(() -> this.loadPaymentController.downloadBlobFileUsingCorrelation("testCorrelationId",
					FileType.PAYMENT835, this.response))
			.isInstanceOf(RuntimeException.class)
			.hasMessage("Unexpected error occurred");
	}

	@Test
	void testDownloadBlobFileUsingPathWhenPathIsNull() {
		Assertions.assertThatThrownBy(() -> this.loadPaymentController.downloadBlobFileUsingPath(null, this.response))
			.isInstanceOf(BadRequestException.class)
			.hasMessage(FILE_PATH_REQUIRED_LOG);
	}

	@Test
	void testDownloadBlobFileUsingPathWhenPathIsMalformed() {
		Assertions
			.assertThatThrownBy(
					() -> this.loadPaymentController.downloadBlobFileUsingPath("../outbound/abc.txt", this.response))
			.isInstanceOf(BadRequestException.class)
			.hasMessage(INVALID_FILE_PATH_LOG + ": ../outbound/abc.txt");
		Mockito.verifyNoInteractions(this.fileService);
	}

	@Test
	void testDownloadBlobFileUsingPath() {
		StreamingResponseBody streamingResponseBody = (outputStream) -> outputStream.write("File content".getBytes());
		Mockito.when(this.fileService.downloadBlobFile("outbound/abc.txt", this.response))
			.thenReturn(streamingResponseBody);
		ResponseEntity<StreamingResponseBody> responseEntity = this.loadPaymentController
			.downloadBlobFileUsingPath("outbound/abc.txt", this.response);
		Assertions.assertThat(responseEntity.getBody()).isNotNull();
		Assertions.assertThat(responseEntity.getStatusCode().is2xxSuccessful()).isTrue();
	}

	@Test
	void testDownloadBlobFileUsingPathWhenFileDoesNotExist() {
		BlobStorageException blobStorageException = mock(BlobStorageException.class);
		BlobStorageError blobStorageError = mock(BlobStorageError.class);
		Mockito.when(blobStorageException.getValue()).thenReturn(blobStorageError);
		Mockito.when(blobStorageError.getCode()).thenReturn("BlobNotFound");
		Mockito.when(this.fileService.downloadBlobFile("outbound/missing.txt", this.response))
			.thenThrow(blobStorageException);

		Assertions
			.assertThatThrownBy(
					() -> this.loadPaymentController.downloadBlobFileUsingPath("outbound/missing.txt", this.response))
			.isInstanceOf(NotFoundException.class)
			.hasMessage(FILE_NOT_FOUND_LOG + ": outbound/missing.txt");
	}

}
