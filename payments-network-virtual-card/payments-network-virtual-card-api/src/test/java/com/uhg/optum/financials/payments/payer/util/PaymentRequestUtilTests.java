package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.repository.AccountRepository;
import com.uhg.optum.financials.payments.payer.repository.OrganizationRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.testUtils.PaymentRequestDtoUtilsTests;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThatExceptionOfType;
import static org.assertj.core.api.AssertionsForClassTypes.assertThatNoException;
import static com.uhg.optum.financials.payments.payer.enums.Status.PI;

@ExtendWith(MockitoExtension.class)
class PaymentRequestUtilTests {

	@Mock
	private OrganizationRepository organizationRegisteredRepository;

	@Mock
	private SubAccountRepository subAccountRepository;

	@Mock
	private AccountRepository payeeRepository;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@InjectMocks
	private PaymentRequestUtil paymentRequestUtil;

	private PaymentRequestDto paymentRequestDto;

	PaymentRequestDtoUtilsTests paymentRequestDtoUtilsTests = new PaymentRequestDtoUtilsTests();

	@BeforeEach
	void setUp() {
		this.paymentRequestDto = this.paymentRequestDtoUtilsTests.getPaymentRequestDto();
	}

	@Test
	void testCheckOrgShortCodeAccountShortCodeSubAccountShortCode_OrgShortCodeNotFound() {
		Mockito.when(this.organizationRegisteredRepository.checkOrgAccountSubAccountExistence(
				this.paymentRequestDto.getOrgShortCode(), this.paymentRequestDto.getAccountShortCode(),
				this.paymentRequestDto.getSubAccountShortCode()))
			.thenReturn(List.of(List.of(false, false, false)));

		assertThatExceptionOfType(BadRequestException.class)
			.isThrownBy(() -> this.paymentRequestUtil.checkOrgAccountSubAccountExistence(this.paymentRequestDto))
			.withMessage("org not found");
	}

	@Test
	void testCheckOrgShortCodeAccountShortCodeSubAccountShortCode_AccountShortCodeNotFound() {
		Mockito.when(this.organizationRegisteredRepository.checkOrgAccountSubAccountExistence(
				this.paymentRequestDto.getOrgShortCode(), this.paymentRequestDto.getAccountShortCode(),
				this.paymentRequestDto.getSubAccountShortCode()))
			.thenReturn(List.of(List.of(true, false, false)));

		assertThatExceptionOfType(BadRequestException.class)
			.isThrownBy(() -> this.paymentRequestUtil.checkOrgAccountSubAccountExistence(this.paymentRequestDto))
			.withMessage("account not found");
	}

	@Test
	void testCheckOrgShortCodeAccountShortCodeSubAccountShortCode_SubAccountShortCodeNotFound() {
		Mockito.when(this.organizationRegisteredRepository.checkOrgAccountSubAccountExistence(
				this.paymentRequestDto.getOrgShortCode(), this.paymentRequestDto.getAccountShortCode(),
				this.paymentRequestDto.getSubAccountShortCode()))
			.thenReturn(List.of(List.of(true, true, false)));
		assertThatExceptionOfType(BadRequestException.class)
			.isThrownBy(() -> this.paymentRequestUtil.checkOrgAccountSubAccountExistence(this.paymentRequestDto))
			.withMessage("subAccount not found");
	}

	@Test
	void testCheckOrgShortCodeAccountShortCodeSubAccountShortCode() {
		Mockito.when(this.organizationRegisteredRepository.checkOrgAccountSubAccountExistence(
				this.paymentRequestDto.getOrgShortCode(), this.paymentRequestDto.getAccountShortCode(),
				this.paymentRequestDto.getSubAccountShortCode()))
			.thenReturn(List.of(List.of(true, true, true)));

		assertThatNoException()
			.isThrownBy(() -> this.paymentRequestUtil.checkOrgAccountSubAccountExistence(this.paymentRequestDto));
	}

	@Test
	void testCheckDuplicatePayment_DuplicatePayment() {
		Mockito
			.when(this.paymentRequestRepository
				.existsByPaymentDetailsId(this.paymentRequestDto.getPaymentDetails().getId()))
			.thenReturn(true);

		assertThatExceptionOfType(BadRequestException.class)
			.isThrownBy(() -> this.paymentRequestUtil.checkDuplicatePayment(this.paymentRequestDto))
			.withMessage("Payment Already Exists");
	}

	@Test
	void testCheckDuplicatePayment() {
		Mockito
			.when(this.paymentRequestRepository
				.existsByPaymentDetailsId(this.paymentRequestDto.getPaymentDetails().getId()))
			.thenReturn(false);

		assertThatNoException().isThrownBy(() -> this.paymentRequestUtil.checkDuplicatePayment(this.paymentRequestDto));
	}

	@Test
	void testValidateMandatoryFields_Success() {
		assertThatNoException()
			.isThrownBy(() -> this.paymentRequestUtil.validateMandatoryFields(this.paymentRequestDto));
	}

	@Test
	void testValidateMandatoryFields_ClaimsNull() {
		this.paymentRequestDto.setClaims(null);
		assertThatExceptionOfType(BadRequestException.class)
			.isThrownBy(() -> this.paymentRequestUtil.validateMandatoryFields(this.paymentRequestDto))
			.withMessage("claims.totalCount is required");
	}

	@Test
	void testValidateMandatoryFields_ClaimsTotalCountNull() {
		this.paymentRequestDto.getClaims().setTotalCount(null);

		assertThatExceptionOfType(BadRequestException.class)
			.isThrownBy(() -> this.paymentRequestUtil.validateMandatoryFields(this.paymentRequestDto))
			.withMessage("claims.totalCount is required");
	}

	@Test
	void testCheckDuplicatePayment_ReprocessPayment() {
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setStatus(PI.name());
		Mockito.when(this.paymentRequestRepository.findByPaymentId(this.paymentRequestDto.getPaymentDetails().getId()))
			.thenReturn(paymentRequest);

		assertThatExceptionOfType(BadRequestException.class)
			.isThrownBy(() -> this.paymentRequestUtil.checkPaymentForReprocessing(this.paymentRequestDto))
			.withMessage("Payment is not in a reprocessable state");
	}

	@Test
	void testCheckDuplicatePayment_ReprocessPaymentNull() {
		Mockito.when(this.paymentRequestRepository.findByPaymentId(this.paymentRequestDto.getPaymentDetails().getId()))
			.thenReturn(null);

		assertThatExceptionOfType(BadRequestException.class)
			.isThrownBy(() -> this.paymentRequestUtil.checkPaymentForReprocessing(this.paymentRequestDto))
			.withMessage("Payment Not Found for Reprocessing");
	}

}
