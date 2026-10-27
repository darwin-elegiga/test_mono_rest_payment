package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.constant.PaymentGroupingLevelType;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.PaymentGroupingLevel;
import com.uhg.optum.financials.payments.payer.service.PaymentGroupingLevelService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;

@ExtendWith(MockitoExtension.class)
class PaymentGroupLevelControllerTests {

	@InjectMocks
	private PaymentGroupLevelController paymentGroupLevelController;

	@Mock
	private LogUtility logUtility;

	@Mock
	private PaymentGroupingLevelService paymentGroupingLevelService;

	@Test
	void savePaymentGroupLevel_success() {
		String orgShortCode = "OXFD5";
		PaymentGroupingLevelType levelType = PaymentGroupingLevelType.ORG;
		PaymentGroupingLevel expected = new PaymentGroupingLevel();

		given(this.paymentGroupingLevelService.savePaymentGroupingLevel(eq(orgShortCode), eq(levelType)))
			.willReturn(expected);

		PaymentGroupingLevel result = this.paymentGroupLevelController.savePaymentGroupLevel(orgShortCode, levelType);

		assertThat(result).isEqualTo(expected);
		then(this.logUtility).should().logMessage(anyString(), anyString(), any(org.slf4j.Logger.class));
	}

	@Test
	void savePaymentGroupLevel_serviceThrowsException() {
		String orgShortCode = "OXFD5";
		PaymentGroupingLevelType levelType = PaymentGroupingLevelType.ORG;

		given(this.paymentGroupingLevelService.savePaymentGroupingLevel(eq(orgShortCode), eq(levelType)))
			.willThrow(new IllegalArgumentException("Invalid grouping level"));

		Assertions
			.assertThatThrownBy(() -> this.paymentGroupLevelController.savePaymentGroupLevel(orgShortCode, levelType))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("Invalid grouping level");
	}

	@Test
	void getPaymentGroupLevel_success() {
		String orgShortCode = "OXFD5";
		PaymentGroupingLevel expected = new PaymentGroupingLevel();

		given(this.paymentGroupingLevelService.getPaymentGroupLevel(eq(orgShortCode))).willReturn(expected);

		PaymentGroupingLevel result = this.paymentGroupLevelController.getPaymentGroupLevel(orgShortCode);

		assertThat(result).isEqualTo(expected);
		then(this.logUtility).should().logMessage(anyString(), anyString(), any(org.slf4j.Logger.class));
	}

	@Test
	void getPaymentGroupLevel_notFound() {
		String orgShortCode = "OXFD5";

		given(this.paymentGroupingLevelService.getPaymentGroupLevel(eq(orgShortCode)))
			.willThrow(new NotFoundException("Organization with orgShortCode " + orgShortCode + " not found."));

		Assertions.assertThatThrownBy(() -> this.paymentGroupLevelController.getPaymentGroupLevel(orgShortCode))
			.isInstanceOf(NotFoundException.class)
			.hasMessage("Organization with orgShortCode " + orgShortCode + " not found.");
		then(this.logUtility).should().logMessage(anyString(), anyString(), any(org.slf4j.Logger.class));
	}

	@Test
	void getPaymentGroupLevel_blankOrgShortCode() {
		given(this.paymentGroupingLevelService.getPaymentGroupLevel(eq("   ")))
			.willThrow(new BadRequestException("Organization short code must be provided and cannot be empty."));

		Assertions.assertThatThrownBy(() -> this.paymentGroupLevelController.getPaymentGroupLevel("   "))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("Organization short code must be provided and cannot be empty.");
	}

	@Test
	void getPaymentGroupLevel_serviceThrowsException() {
		String orgShortCode = "OXFD5";

		given(this.paymentGroupingLevelService.getPaymentGroupLevel(eq(orgShortCode)))
			.willThrow(new RuntimeException("Service error"));

		Assertions.assertThatThrownBy(() -> this.paymentGroupLevelController.getPaymentGroupLevel(orgShortCode))
			.isInstanceOf(RuntimeException.class)
			.hasMessageContaining("Service error");
	}

}
