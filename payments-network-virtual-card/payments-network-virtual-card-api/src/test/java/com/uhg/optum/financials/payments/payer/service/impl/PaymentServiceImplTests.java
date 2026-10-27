package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.PaymentDetailsResponseDto;
import com.uhg.optum.financials.payments.payer.dto.FileTrackingDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.exception.QueuePublishException;
import com.uhg.optum.financials.payments.payer.exception.ServiceUnavailableException;
import com.uhg.optum.financials.payments.payer.model.FileTracking;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.repository.FileTrackingRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.LoadPaymentCardCallbackService;
import com.uhg.optum.financials.payments.payer.service.StorageQueueProducer;
import com.uhg.optum.financials.payments.payer.testUtils.PaymentRequestDtoUtilsTests;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.PaymentRequestUtil;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Optional;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.ArgumentMatchers.eq;

@ExtendWith(MockitoExtension.class)
public class PaymentServiceImplTests {

	@InjectMocks
	private PaymentServiceImpl paymentServiceImpl;

	@Mock
	private PaymentRequestUtil paymentRequestUtil;

	@Mock
	private LogUtility logUtility;

	@Mock
	private StorageQueueProducer storageQueueProducer;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@Mock
	private FileTrackingRepository fileTrackingRepository;

	@Mock
	private LoadPaymentCardCallbackService loadPaymentCardCallbackService;

	PaymentRequestDtoUtilsTests paymentRequestDtoUtilsTests = new PaymentRequestDtoUtilsTests();

	@Test
	void loadPayment() {
		PaymentRequestDto paymentRequestDto = new PaymentRequestDto();
		PaymentDetailsDto paymentDetails = new PaymentDetailsDto();
		paymentDetails.setId("payment-1");
		paymentRequestDto.setPaymentDetails(paymentDetails);
		this.paymentServiceImpl.loadPayment(paymentRequestDto);
		Mockito.verify(this.paymentRequestUtil, Mockito.times(1)).checkDuplicatePayment(paymentRequestDto);
		Mockito.verify(this.loadPaymentCardCallbackService).pollAndCallback("payment-1");
	}

	@Test
	void loadPaymentException() {
		PaymentRequestDto paymentRequestDto = new PaymentRequestDto();
		PaymentDetailsDto paymentDetails = new PaymentDetailsDto();
		paymentDetails.setId("12345");
		paymentRequestDto.setPaymentDetails(paymentDetails);
		Mockito.doThrow(BadRequestException.class).when(this.storageQueueProducer).sendPaymentEventToQueue(any());
		Assertions.assertThat(Assertions.catchThrowable(() -> this.paymentServiceImpl.loadPayment(paymentRequestDto)))
			.isInstanceOf(BadRequestException.class);
		Mockito.verify(this.paymentRequestUtil, Mockito.times(1)).checkDuplicatePayment(paymentRequestDto);
		Mockito.verify(this.loadPaymentCardCallbackService, Mockito.never()).pollAndCallback(anyString());
	}

	@Test
	void loadPaymentQueuePublishException_returnsServiceUnavailableException() {
		PaymentRequestDto paymentRequestDto = this.paymentRequestDtoUtilsTests.getPaymentRequestDto();
		Mockito.doNothing().when(this.paymentRequestUtil).checkDuplicatePayment(paymentRequestDto);
		Mockito.doNothing().when(this.paymentRequestUtil).checkOrgAccountSubAccountExistence(paymentRequestDto);
		Mockito.doNothing().when(this.paymentRequestUtil).validateMandatoryFields(paymentRequestDto);
		Mockito.doThrow(new QueuePublishException("Queue unavailable", new RuntimeException("connection refused")))
			.when(this.storageQueueProducer)
			.sendPaymentEventToQueue(paymentRequestDto);

		Assertions.assertThat(Assertions.catchThrowable(() -> this.paymentServiceImpl.loadPayment(paymentRequestDto)))
			.isInstanceOf(ServiceUnavailableException.class)
			.hasMessageContaining("Queue unavailable");
	}

	@Test
	void getPaymentRequestDetailException() {
		String paymentID = "12345";
		Assertions
			.assertThat(Assertions.catchThrowable(() -> this.paymentServiceImpl.getPaymentRequestDetail(paymentID)))
			.isInstanceOf(BadRequestException.class);
		Mockito.verify(this.paymentRequestRepository, Mockito.times(1)).findByPaymentId(paymentID);
	}

	@Test
	void getPaymentRequestDetail() {
		String paymentID = "123456";
		Mockito.when(this.paymentRequestRepository.findByPaymentId(paymentID)).thenReturn(new PaymentRequest());
		this.paymentServiceImpl.getPaymentRequestDetail(paymentID);
		Mockito.verify(this.paymentRequestRepository, Mockito.times(1)).findByPaymentId(paymentID);
	}

	@Test
	void getPaymentRequestDetailIncludesNetworkPaymentId() {
		String paymentID = "123456";
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setId(1L);
		paymentRequest.setStatus("PC");
		paymentRequest.setRePreferenceFlag(true);
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("payment-details-id");
		paymentDetails.setNetworkPaymentId("network-123");
		paymentRequest.setPaymentDetails(paymentDetails);
		Mockito.when(this.paymentRequestRepository.findByPaymentId(paymentID)).thenReturn(paymentRequest);

		var response = this.paymentServiceImpl.getPaymentRequestDetail(paymentID);

		Assertions.assertThat(response.getPaymentDetails()).isInstanceOf(PaymentDetailsResponseDto.class);
		Assertions.assertThat(((PaymentDetailsResponseDto) response.getPaymentDetails()).getNetworkPaymentId())
			.isEqualTo("network-123");
		Assertions.assertThat(response.isRePreferenced()).isTrue();
	}

	@Test
	void testPublishPaymentEvent_BadRequestException() {
		PaymentRequestDto paymentRequestDto = this.paymentRequestDtoUtilsTests.getPaymentRequestDto();
		Mockito.doNothing().when(this.paymentRequestUtil).checkDuplicatePayment(paymentRequestDto);
		Mockito.doNothing().when(this.paymentRequestUtil).checkOrgAccountSubAccountExistence(paymentRequestDto);
		Mockito.doNothing().when(this.paymentRequestUtil).validateMandatoryFields(paymentRequestDto);
		Mockito.doThrow(new BadRequestException("Exception occurred while loading payment"))
			.when(this.storageQueueProducer)
			.sendPaymentEventToQueue(any(PaymentRequestDto.class));

		Assertions.assertThatException().isThrownBy(() -> this.paymentServiceImpl.loadPayment(paymentRequestDto));
	}

	@Test
	void reProcessPayment() {
		PaymentRequestDto paymentRequestDto = new PaymentRequestDto();
		this.paymentServiceImpl.reProcessPayment(paymentRequestDto);
		Mockito.verify(this.paymentRequestUtil, Mockito.times(1)).checkPaymentForReprocessing(paymentRequestDto);
	}

	@Test
	void reProcessPaymentException() {
		PaymentRequestDto paymentRequestDto = new PaymentRequestDto();
		PaymentDetailsDto paymentDetails = new PaymentDetailsDto();
		paymentDetails.setId("payment-1");
		paymentRequestDto.setPaymentDetails(paymentDetails);

		// Mock the PaymentRequest for error message capture
		PaymentRequest paymentRequest = new PaymentRequest();
		Mockito.when(this.paymentRequestRepository.findByPaymentId("payment-1")).thenReturn(paymentRequest);

		Mockito.doThrow(BadRequestException.class).when(this.storageQueueProducer).sendPaymentEventToQueue(any());
		Assertions
			.assertThat(Assertions.catchThrowable(() -> this.paymentServiceImpl.reProcessPayment(paymentRequestDto)))
			.isInstanceOf(BadRequestException.class);
		Mockito.verify(this.paymentRequestUtil, Mockito.times(1)).checkPaymentForReprocessing(paymentRequestDto);
		// Verify that error message was recorded
		Mockito.verify(this.paymentRequestRepository, Mockito.times(1)).save(paymentRequest);
	}

	@Test
	void reProcessPaymentQueuePublishException_returnsServiceUnavailableException() {
		PaymentRequestDto paymentRequestDto = this.paymentRequestDtoUtilsTests.getPaymentRequestDto();
		Mockito.doNothing().when(this.paymentRequestUtil).checkPaymentForReprocessing(paymentRequestDto);
		Mockito.doNothing().when(this.paymentRequestUtil).checkOrgAccountSubAccountExistence(paymentRequestDto);
		Mockito.doNothing().when(this.paymentRequestUtil).validateMandatoryFields(paymentRequestDto);
		Mockito.doThrow(new QueuePublishException("Queue unavailable", new RuntimeException("connection refused")))
			.when(this.storageQueueProducer)
			.sendPaymentEventToQueue(paymentRequestDto);

		Assertions
			.assertThat(Assertions.catchThrowable(() -> this.paymentServiceImpl.reProcessPayment(paymentRequestDto)))
			.isInstanceOf(ServiceUnavailableException.class)
			.hasMessageContaining("Queue unavailable");
		Mockito.verify(this.logUtility)
			.logMessageWithPriority(
					org.mockito.ArgumentMatchers.startsWith("Exception while sending the payment message to the queue"),
					eq("error"), any(), eq("P3"));
	}

	@Test
	void getFileName_Success() {
		// Arrange
		String correlationId = "test-correlation-id";
		FileTracking fileTracking = Mockito.mock(FileTracking.class);
		FileTrackingDto fileTrackingDto = new FileTrackingDto();
		Mockito.when(this.fileTrackingRepository.findByCorrelationId(correlationId))
			.thenReturn(Optional.of(fileTracking));
		Mockito.when(fileTracking.toDto(Mockito.any(FileTrackingDto.class))).thenReturn(fileTrackingDto);
		FileTrackingDto result = this.paymentServiceImpl.getFileName(correlationId);

		// Assert
		Assertions.assertThat(result).isEqualTo(fileTrackingDto);
		Mockito.verify(this.fileTrackingRepository, Mockito.times(1)).findByCorrelationId(correlationId);
		Mockito.verify(fileTracking, Mockito.times(1)).toDto(Mockito.any(FileTrackingDto.class));
	}

	@Test
	void getFileName_NotFound() {
		// Arrange
		String correlationId = "non-existent-correlation-id";
		Mockito.when(this.fileTrackingRepository.findByCorrelationId(anyString())).thenReturn(Optional.empty());

		// Act & Assert
		Assertions.assertThatThrownBy(() -> this.paymentServiceImpl.getFileName(correlationId))
			.isInstanceOf(NotFoundException.class)
			.hasMessageContaining("No file found for correlation ID: " + correlationId);
		Mockito.verify(this.fileTrackingRepository, Mockito.times(1)).findByCorrelationId(correlationId);
	}

}
