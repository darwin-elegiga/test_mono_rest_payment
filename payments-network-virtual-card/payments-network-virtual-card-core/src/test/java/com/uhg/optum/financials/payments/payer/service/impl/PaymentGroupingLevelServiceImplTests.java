package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.PaymentGroupingLevelType;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.PaymentGroupingLevel;
import com.uhg.optum.financials.payments.payer.repository.PaymentGroupingLevelRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Optional;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.verify;

@ExtendWith(MockitoExtension.class)
class PaymentGroupingLevelServiceImplTests {

	@Mock
	private PaymentGroupingLevelRepository paymentGroupingLevelRepository;

	@InjectMocks
	private PaymentGroupingLevelServiceImpl paymentGroupingLevelServiceImpl;

	@Test
	void savePaymentGroupingLevel_shouldCreateNewIfNotExists() {
		String orgShortCode = "ORG1";
		PaymentGroupingLevelType type = PaymentGroupingLevelType.ORG;
		given(this.paymentGroupingLevelRepository.findByOrgShortCode(orgShortCode)).willReturn(Optional.empty());
		// Return the argument passed to save
		given(this.paymentGroupingLevelRepository.save(any(PaymentGroupingLevel.class)))
			.willAnswer((invocation) -> invocation.getArgument(0));

		PaymentGroupingLevel result = this.paymentGroupingLevelServiceImpl.savePaymentGroupingLevel(orgShortCode, type);
		assertThat(result).isNotNull();
		assertThat(result.getOrgShortCode()).isEqualTo(orgShortCode);
		assertThat(result.getGroupLevel()).isEqualTo(type.name());
		assertThat(result.getCreatedBy()).isEqualTo(VIRTUAL_CARD);
		assertThat(result.getCreatedOn()).isNotNull();
		verify(this.paymentGroupingLevelRepository).save(result);
	}

	@Test
	void savePaymentGroupingLevel_shouldUpdateIfExists() {
		String orgShortCode = "ORG2";
		PaymentGroupingLevelType type = PaymentGroupingLevelType.ACCOUNT;
		PaymentGroupingLevel existing = new PaymentGroupingLevel();
		existing.setOrgShortCode(orgShortCode);
		given(this.paymentGroupingLevelRepository.findByOrgShortCode(orgShortCode)).willReturn(Optional.of(existing));
		given(this.paymentGroupingLevelRepository.save(existing)).willReturn(existing);

		PaymentGroupingLevel result = this.paymentGroupingLevelServiceImpl.savePaymentGroupingLevel(orgShortCode, type);
		assertThat(result).isNotNull();
		assertThat(result.getOrgShortCode()).isEqualTo(orgShortCode);
		assertThat(result.getGroupLevel()).isEqualTo(type.name());
		assertThat(result.getUpdatedBy()).isEqualTo(VIRTUAL_CARD);
		assertThat(result.getUpdatedOn()).isNotNull();
		verify(this.paymentGroupingLevelRepository).save(result);
	}

	@Test
	void getPaymentGroupLevel_shouldReturnEntity() {
		String orgShortCode = "ORG3";
		PaymentGroupingLevel expected = new PaymentGroupingLevel();
		expected.setOrgShortCode(orgShortCode);
		given(this.paymentGroupingLevelRepository.findByOrgShortCode(orgShortCode)).willReturn(Optional.of(expected));

		PaymentGroupingLevel result = this.paymentGroupingLevelServiceImpl.getPaymentGroupLevel(orgShortCode);
		assertThat(result.getOrgShortCode()).isEqualTo(orgShortCode);
	}

	@Test
	void getPaymentGroupLevel_shouldThrowNotFoundException() {
		String orgShortCode = "ORG4";
		given(this.paymentGroupingLevelRepository.findByOrgShortCode(orgShortCode)).willReturn(Optional.empty());

		assertThatThrownBy(() -> this.paymentGroupingLevelServiceImpl.getPaymentGroupLevel(orgShortCode))
			.isInstanceOf(NotFoundException.class)
			.hasMessage("Organization with orgShortCode " + orgShortCode + " not found.");
	}

	@Test
	void getPaymentGroupLevel_shouldThrowBadRequestException_whenOrgShortCodeBlank() {
		assertThatThrownBy(() -> this.paymentGroupingLevelServiceImpl.getPaymentGroupLevel("   "))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("Organization short code must be provided and cannot be empty.");
	}

}
