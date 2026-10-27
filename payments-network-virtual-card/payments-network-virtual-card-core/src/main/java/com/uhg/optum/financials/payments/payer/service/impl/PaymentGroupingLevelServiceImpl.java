package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.PaymentGroupingLevelType;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.PaymentGroupingLevel;
import com.uhg.optum.financials.payments.payer.repository.PaymentGroupingLevelRepository;
import com.uhg.optum.financials.payments.payer.service.PaymentGroupingLevelService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.Optional;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;

@Service
@Slf4j
public class PaymentGroupingLevelServiceImpl implements PaymentGroupingLevelService {

	private final PaymentGroupingLevelRepository paymentGroupingLevelRepository;

	public PaymentGroupingLevelServiceImpl(PaymentGroupingLevelRepository paymentGroupingLevelRepository) {
		this.paymentGroupingLevelRepository = paymentGroupingLevelRepository;
	}

	@Override
	public PaymentGroupingLevel savePaymentGroupingLevel(String orgShortCode,
			PaymentGroupingLevelType paymentGroupingLevelType) {
		Optional<PaymentGroupingLevel> paymentGroupingLevel = this.paymentGroupingLevelRepository
			.findByOrgShortCode(orgShortCode);

		if (paymentGroupingLevel.isEmpty()) {
			paymentGroupingLevel = Optional.of(new PaymentGroupingLevel());
			paymentGroupingLevel.get().setOrgShortCode(orgShortCode);
			paymentGroupingLevel.get().setCreatedBy(VIRTUAL_CARD);
			paymentGroupingLevel.get().setCreatedOn(LocalDateTime.now());
		}
		else {
			paymentGroupingLevel.get().setUpdatedBy(VIRTUAL_CARD);
			paymentGroupingLevel.get().setUpdatedOn(LocalDateTime.now());
		}

		paymentGroupingLevel.get().setGroupLevel(paymentGroupingLevelType.name());
		return this.paymentGroupingLevelRepository.save(paymentGroupingLevel.get());
	}

	@Override
	public PaymentGroupingLevel getPaymentGroupLevel(String orgShortCode) {
		if (orgShortCode == null || orgShortCode.isBlank()) {
			throw new BadRequestException("Organization short code must be provided and cannot be empty.");
		}

		return this.paymentGroupingLevelRepository.findByOrgShortCode(orgShortCode)
			.orElseThrow(() -> new NotFoundException("Organization with orgShortCode " + orgShortCode + " not found."));
	}

}
