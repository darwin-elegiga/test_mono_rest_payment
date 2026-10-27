package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountBankAccountDetails;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.Address;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.service.EncryptionAtRestService;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.client.dto.payment.SubAccountDetailsDto;
import com.uhg.optum.financials.shared.lib835.service.SegmentBuilderService;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.test.util.ReflectionTestUtils;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.ArgumentMatchers.isNull;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.verify;

@ExtendWith(MockitoExtension.class)
class SegmentCreationServiceImplTests {

	@InjectMocks
	private SegmentCreationServiceImpl segmentCreationService;

	@Mock
	private SegmentBuilderService segmentBuilder;

	@Mock
	private EncryptionAtRestService encryptionAtRestService;

	@BeforeEach
	void setUp() {
		ReflectionTestUtils.setField(this.segmentCreationService, "holidaysList", "2024-01-01|2024-01-15");
	}

	@Test
	void createSegment_withExplicitNetworkId_usesTrnOverride() {
		PaymentRequestDto requestDto = buildRequest();
		SubAccountBankAccountDetails subAccountDetails = buildSubAccountDetails();

		given(this.encryptionAtRestService.decrypt("encryptedAccountNumber")).willReturn("decryptedAccountNumber");
		given(this.segmentBuilder.buildSTSESegment(eq(requestDto), any(SubAccountDetailsDto.class),
				eq("VC-B2B-0000000042")))
			.willReturn("STSE-STRING");

		String result = this.segmentCreationService.createSegment(requestDto, subAccountDetails, null,
				"VC-B2B-0000000042");

		Assertions.assertThat(result).isEqualTo("STSE-STRING");
		verify(this.segmentBuilder).buildSTSESegment(eq(requestDto), any(SubAccountDetailsDto.class),
				eq("VC-B2B-0000000042"));
	}

	@Test
	void createSegment_withNullNetworkId_fallsBackToKernelDefaultTrn() {
		PaymentRequestDto requestDto = buildRequest();
		SubAccountBankAccountDetails subAccountDetails = buildSubAccountDetails();

		given(this.encryptionAtRestService.decrypt("encryptedAccountNumber")).willReturn("decryptedAccountNumber");
		given(this.segmentBuilder.buildSTSESegment(eq(requestDto), any(SubAccountDetailsDto.class), isNull()))
			.willReturn("STSE-STRING");

		String result = this.segmentCreationService.createSegment(requestDto, subAccountDetails, null, null);

		Assertions.assertThat(result).isEqualTo("STSE-STRING");
		verify(this.segmentBuilder).buildSTSESegment(eq(requestDto), any(SubAccountDetailsDto.class), isNull());
	}

	@Test
	void createSegment_withBlankNetworkId_fallsBackToKernelDefaultTrn() {
		PaymentRequestDto requestDto = buildRequest();
		SubAccountBankAccountDetails subAccountDetails = buildSubAccountDetails();

		given(this.encryptionAtRestService.decrypt("encryptedAccountNumber")).willReturn("decryptedAccountNumber");
		given(this.segmentBuilder.buildSTSESegment(eq(requestDto), any(SubAccountDetailsDto.class), eq("   ")))
			.willReturn("STSE-STRING");

		String result = this.segmentCreationService.createSegment(requestDto, subAccountDetails, null, "   ");

		Assertions.assertThat(result).isEqualTo("STSE-STRING");
		verify(this.segmentBuilder).buildSTSESegment(eq(requestDto), any(SubAccountDetailsDto.class), eq("   "));
	}

	@Test
	void createSegment_bankAccountDetailsNotFound_throwsException() {
		PaymentRequestDto requestDto = new PaymentRequestDto();

		Assertions.assertThatThrownBy(() -> this.segmentCreationService.createSegment(requestDto, null, null, "NET-1"))
			.isInstanceOf(NotFoundException.class)
			.hasMessage("Bank account details not found to process payment");
	}

	@Test
	void createSegment_bankAccountNumberNull_throwsException() {
		PaymentRequestDto requestDto = new PaymentRequestDto();
		SubAccountBankAccountDetails subAccountDetails = new SubAccountBankAccountDetails();

		Assertions
			.assertThatThrownBy(
					() -> this.segmentCreationService.createSegment(requestDto, subAccountDetails, null, "NET-1"))
			.isInstanceOf(NotFoundException.class)
			.hasMessage("Bank account details not found to process payment");
	}

	private PaymentRequestDto buildRequest() {
		PaymentRequestDto requestDto = new PaymentRequestDto();
		PaymentDetailsDto paymentDetails = new PaymentDetailsDto();
		paymentDetails.setSettlementTime("000000");
		paymentDetails.setId("EXT-ID-001");
		requestDto.setPaymentDetails(paymentDetails);
		requestDto.setOrgShortCode("ORG001");
		requestDto.setSubAccountShortCode("SUB001");
		return requestDto;
	}

	private SubAccountBankAccountDetails buildSubAccountDetails() {
		SubAccountBankAccountDetails subAccountDetails = new SubAccountBankAccountDetails();
		subAccountDetails.setBankAccountNumber("encryptedAccountNumber");
		subAccountDetails.setPayer835Id("payer835Id");
		SubAccount subAccount = new SubAccount();
		subAccount.setTin("123456789");
		subAccount.setName("Test SubAccount");
		subAccount.setAddress(new Address());
		subAccountDetails.setSubAccount(subAccount);
		return subAccountDetails;
	}

}
