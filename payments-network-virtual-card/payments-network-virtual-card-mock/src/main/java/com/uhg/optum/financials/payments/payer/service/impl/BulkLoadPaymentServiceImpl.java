package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.dto.BulkLoadPaymentRequestDto;
import com.uhg.optum.financials.payments.payer.dto.BulkLoadPaymentResponseDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.service.BulkLoadPaymentService;
import com.uhg.optum.financials.shared.client.constant.PayeeType;
import com.uhg.optum.financials.shared.client.constant.PaymentType;
import com.uhg.optum.financials.shared.client.constant.RemitMethod;
import com.uhg.optum.financials.shared.client.dto.payment.AddressDto;
import com.uhg.optum.financials.shared.client.dto.payment.ClaimsDto;
import com.uhg.optum.financials.shared.client.dto.payment.DocumentDto;
import com.uhg.optum.financials.shared.client.dto.payment.PayeeDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.client.dto.payment.ShippingDetailsDto;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;
import org.springframework.web.client.HttpStatusCodeException;
import org.springframework.web.client.RestTemplate;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.ThreadLocalRandom;

@Slf4j
@Service
public class BulkLoadPaymentServiceImpl implements BulkLoadPaymentService {

	private final String loadPaymentUrl;

	private final RestTemplate restTemplate = new RestTemplate();

	public BulkLoadPaymentServiceImpl(@Value("${endpoint.loadPayment.url}") String loadPaymentUrl) {
		this.loadPaymentUrl = loadPaymentUrl;
	}

	@Override
	public BulkLoadPaymentResponseDto bulkLoadPayments(BulkLoadPaymentRequestDto request) {
		validateBulkLoadRequest(request);

		String bulkCorrelationId = UUID.randomUUID().toString().replace("-", "");
		List<BulkLoadPaymentResponseDto.PaymentEntry> paymentEntries = new ArrayList<>();

		log.info(
				"[correlationId={}] Starting bulk load of {} payment(s) for org={}, account={}, subAccount={}, type={}",
				bulkCorrelationId, request.getNumberOfPayments(), request.getOrgShortCode(),
				request.getAccountShortCode(), request.getSubAccountShortCode(), request.getPaymentType());

		int total = request.getNumberOfPayments();
		int maxConcurrent = 200; // tune
		ExecutorService pool = Executors.newFixedThreadPool(maxConcurrent);

		for (int i = 0; i < total; i++) {
			String paymentId = String.valueOf(ThreadLocalRandom.current().nextInt(100000000, 1000000000));
			PaymentRequestDto dto = buildPaymentRequestDto(request, paymentId, bulkCorrelationId);
			paymentEntries.add(new BulkLoadPaymentResponseDto.PaymentEntry(paymentId));

			CompletableFuture.runAsync(() -> {
				try {
					invokeLoadPaymentApi(dto);
				}
				catch (Exception ex) {
					log.error("Async load payment failed for paymentId={}: {}", paymentId, ex.getMessage());
				}
			}, pool);
		}
		pool.shutdown();
		// return immediately (accepted semantics)
		// return new BulkLoadPaymentResponseDto("ACCEPTED", bulkCorrelationId,
		// paymentEntries.size(), paymentEntries);

		log.info("[correlationId={}] Bulk load completed. Total payments triggered: {}", bulkCorrelationId,
				paymentEntries.size());

		return new BulkLoadPaymentResponseDto("SUCCESS", bulkCorrelationId, paymentEntries.size(), paymentEntries);
	}

	private void validateBulkLoadRequest(BulkLoadPaymentRequestDto request) {
		if (request == null) {
			throw new BadRequestException("Bulk load request is required");
		}
		if (request.getNumberOfPayments() == null || request.getNumberOfPayments() <= 0) {
			throw new BadRequestException("numberOfPayments must be greater than 0");
		}
		if (!StringUtils.hasText(request.getOrgShortCode()) || !StringUtils.hasText(request.getAccountShortCode())
				|| !StringUtils.hasText(request.getSubAccountShortCode())) {
			throw new BadRequestException("orgShortCode, accountShortCode and subAccountShortCode are required");
		}
		if (request.getPaymentType() == null) {
			throw new BadRequestException("paymentType is required");
		}
	}

	private String normalizeShortCode(String value) {
		return (value != null) ? value.trim() : null;
	}

	private void invokeLoadPaymentApi(PaymentRequestDto dto) {
		HttpHeaders headers = new HttpHeaders();
		headers.setContentType(MediaType.APPLICATION_JSON);

		HttpEntity<PaymentRequestDto> requestEntity = new HttpEntity<>(dto, headers);

		try {
			log.debug("Invoking load payment API at: {}", this.loadPaymentUrl);
			ResponseEntity<Void> response = this.restTemplate.postForEntity(this.loadPaymentUrl, requestEntity,
					Void.class);

			if (!response.getStatusCode().is2xxSuccessful()) {
				log.error("Load payment API failed with status: {}", response.getStatusCode());
				throw new BadRequestException("Load payment API failed with status: " + response.getStatusCode());
			}
			log.debug("Load payment API call succeeded");
		}
		catch (HttpStatusCodeException ex) {
			log.error("Load payment API HTTP error: {} - {}", ex.getStatusCode(), ex.getResponseBodyAsString());
			throw new BadRequestException(
					"Load payment API error: " + ex.getStatusCode() + " - " + ex.getResponseBodyAsString());
		}
		catch (BadRequestException ex) {
			throw ex;
		}
		catch (Exception ex) {
			log.error("Load payment API invocation failed: {}", ex.getMessage(), ex);
			throw new BadRequestException("Load payment API invocation failed: " + ex.getMessage());
		}
	}

	private PaymentRequestDto buildPaymentRequestDto(BulkLoadPaymentRequestDto request, String paymentId,
			String correlationId) {
		String payeeTypeCode = (request.getPaymentType() == BulkLoadPaymentRequestDto.PaymentCategory.C2B)
				? PayeeType.C.name() : PayeeType.B.name();
		String internalPaymentId = String.valueOf(ThreadLocalRandom.current().nextInt(100000000, 1000000000));

		PaymentDetailsDto paymentDetails = new PaymentDetailsDto();
		paymentDetails.setId(internalPaymentId);
		paymentDetails.setIssuerPaymentId(paymentId);
		paymentDetails.setReferenceId(paymentId);
		paymentDetails.setPaymentDate(LocalDate.now().toString());
		paymentDetails.setAdjudicationCutoffDate(LocalDate.now().toString());
		paymentDetails.setAdjudicationVersion("1.0");
		paymentDetails.setAmount(BigDecimal.valueOf(100.00));
		paymentDetails.setRemitMethod(RemitMethod.PDF.name());
		paymentDetails.setJurisdictionalBody("US");
		paymentDetails.setNumberOfAttachedPdfs(1);
		paymentDetails.setType(PaymentType.PAYMENT);
		paymentDetails.setPin("uFMA1oMiPbepwSSMZEPw");
		paymentDetails.setSettlementTime("000000");

		AddressDto address = new AddressDto();
		address.setLine1("123 Main St");
		address.setLine2("Apt 1");
		address.setCity("Minneapolis");
		address.setState("MN");
		address.setPostalCode("55401");
		address.setCountry("US");

		PayeeDetailsDto payeeDetails = new PayeeDetailsDto();
		payeeDetails.setType(payeeTypeCode);
		payeeDetails.setName("Bulk Payment Payee");
		payeeDetails.setTaxId("123456789");
		payeeDetails.setNpi("1234567890");
		payeeDetails.setReceiverId(UUID.randomUUID().toString().replace("-", "").substring(0, 10));
		payeeDetails.setAddress(address);

		ShippingDetailsDto shippingDetails = new ShippingDetailsDto();
		shippingDetails.setName("Bulk Payment Payee");
		shippingDetails.setAddress(address);

		ClaimsDto claims = new ClaimsDto();
		claims.setTotalCount(1);
		claims.setUrl("http://localhost:8080/claims");

		DocumentDto document = new DocumentDto();
		document.setUrl(AppConstants.URL);
		document.setDocType("pdf");

		PaymentRequestDto dto = new PaymentRequestDto();
		dto.setOrigin("Issuing");
		dto.setCorrelationId(correlationId);
		dto.setOrgShortCode(normalizeShortCode(request.getOrgShortCode()));
		dto.setAccountShortCode(normalizeShortCode(request.getAccountShortCode()));
		dto.setSubAccountShortCode(normalizeShortCode(request.getSubAccountShortCode()));
		dto.setPaymentDetails(paymentDetails);
		dto.setPayeeDetails(payeeDetails);
		dto.setShippingDetails(shippingDetails);
		dto.setClaims(claims);
		dto.setDocuments(List.of(document));

		return dto;
	}

}
