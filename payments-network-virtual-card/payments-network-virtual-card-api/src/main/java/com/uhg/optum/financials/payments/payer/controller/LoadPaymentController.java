package com.uhg.optum.financials.payments.payer.controller;

import com.azure.storage.blob.implementation.models.BlobStorageError;
import com.azure.storage.blob.models.BlobStorageException;
import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.constant.FileType;
import com.uhg.optum.financials.payments.payer.dto.FileTrackingDto;
import com.uhg.optum.financials.payments.payer.dto.PaymentResponseDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.service.FileService;
import com.uhg.optum.financials.payments.payer.service.PaymentService;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.validation.Valid;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.util.StringUtils;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.servlet.mvc.method.annotation.StreamingResponseBody;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.FILE_NOT_FOUND_LOG;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.FILE_PATH_REQUIRED_LOG;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.INVALID_FILE_PATH_LOG;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.SUCCESS;

@RestController
@Slf4j
@Tag(name = "Payments")
@RequestMapping("/v1/payment")
public class LoadPaymentController {

	@Autowired
	private PaymentService paymentService;

	@Autowired
	LogUtility logUtility;

	@Autowired
	AuditLogger auditLogger;

	@Autowired
	private FileService fileService;

	@Operation(summary = "Load a Payment",
			description = " **NOTE**: Accepts a comprehensive payment request and sends it to the Storage Queue for processing. This endpoint is used to initiate the payment workflow and supports detailed payment, payee, shipping, claims, and document information. It ensures that all necessary data is captured for processing.")
	@PreAuthorize("hasAuthority('org:networks:write')")
	@PostMapping("/load")
	public ResponseEntity<String> loadPayment(@Valid @RequestBody PaymentRequestDto paymentRequestDto) {
		this.logUtility.logMessage(
				String.format("[correlationId=%s] [paymentId=%s] API Module- Start processing payment request",
						paymentRequestDto.getCorrelationId(), paymentRequestDto.getPaymentDetails().getId()),
				AppConstants.INFO, log);
		this.auditLogger.logCreateReceived(paymentRequestDto.getPaymentDetails().getId());
		this.paymentService.loadPayment(paymentRequestDto);
		this.logUtility.logMessage(
				String.format("[correlationId=%s] [paymentId=%s] API Module - Finished processing payment request",
						paymentRequestDto.getCorrelationId(), paymentRequestDto.getPaymentDetails().getId()),
				AppConstants.INFO, log);

		return new ResponseEntity<>(SUCCESS, HttpStatus.OK);
	}

	@Operation(summary = "Get payment details",
			description = "**NOTE:** Retrieves detailed information about a specific payment using the provided payment ID.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/{paymentID}")
	public ResponseEntity<PaymentResponseDto> getPaymentRequestDetail(@PathVariable String paymentID) {
		return ResponseEntity.ok(this.paymentService.getPaymentRequestDetail(paymentID));
	}

	@Operation(summary = "Get File Name",
			description = "**NOTE:** Get 835, and Companion file names using correlation id")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("get-files/{correlationId}")
	public ResponseEntity<FileTrackingDto> getFileName(@PathVariable String correlationId) {
		return ResponseEntity.ok(this.paymentService.getFileName(correlationId));
	}

	@Operation(summary = "Downloads file by path",
			description = "**NOTE:** Downloads a specific file associated with a payment transaction using the file path and file type. Supported file types include: 835 remittance files, funding files, EOB ZIPs, AP, C835, and companion files.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/download/file/by-path")
	public ResponseEntity<StreamingResponseBody> downloadBlobFileUsingPath(
			@RequestParam(required = false) String filePath, HttpServletResponse response) {
		if (!StringUtils.hasText(filePath)) {
			throw new BadRequestException(FILE_PATH_REQUIRED_LOG);
		}
		if (!isValidFilePath(filePath)) {
			throw new BadRequestException(INVALID_FILE_PATH_LOG + ": " + filePath);
		}
		return downloadFile(filePath, response);
	}

	@Operation(summary = "Downloads file by name",
			description = "**NOTE:** Downloads a specific file associated with a payment transaction using the correlation ID and file type.Supported file types include: 835 remittance files, and companion files.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/download/file/by-correlation/{correlationId}/{fileType}")
	public ResponseEntity<StreamingResponseBody> downloadBlobFileUsingCorrelation(@PathVariable String correlationId,
			@PathVariable FileType fileType, HttpServletResponse response) {

		FileTrackingDto fileNames = this.paymentService.getFileName(correlationId);
		String location = null;

		switch (fileType) {
			case PAYMENT835:
				location = fileNames.get_835();
				break;
			case COMPANION:
				location = fileNames.getCompanion();
				break;
			default:
				break;
		}

		if (location == null) {
			throw new NotFoundException(FILE_NOT_FOUND_LOG);
		}
		return downloadFile(location, response);
	}

	private ResponseEntity<StreamingResponseBody> downloadFile(String location, HttpServletResponse response) {
		try {
			return ResponseEntity.ok(this.fileService.downloadBlobFile(location, response));
		}
		catch (BlobStorageException ex) {
			BlobStorageError error = (BlobStorageError) ex.getValue();
			if (error != null && "BlobNotFound".equalsIgnoreCase(error.getCode())) {
				log.error("Blob not found: {}", ex.getMessage());
				throw new NotFoundException(FILE_NOT_FOUND_LOG + ": " + location);
			}
			throw ex;
		}
		catch (Exception ex) {
			log.error("Error while downloading file: {}", ex.getMessage());
			throw new InternalServerException(ex.getMessage());
		}
	}

	private boolean isValidFilePath(String filePath) {
		String trimmedPath = filePath.trim();
		return !trimmedPath.contains("..") && !trimmedPath.contains("\\") && !trimmedPath.contains("//");
	}

}
