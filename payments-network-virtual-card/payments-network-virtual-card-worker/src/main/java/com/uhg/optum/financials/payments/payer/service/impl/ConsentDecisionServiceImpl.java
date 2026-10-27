package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.repository.ConsentRequiuredStateRepository;
import com.uhg.optum.financials.payments.payer.repository.ProviderStateConsentRepository;
import com.uhg.optum.financials.payments.payer.service.ConsentDecisionService;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;

import java.util.Locale;

@Service
public class ConsentDecisionServiceImpl implements ConsentDecisionService {

	private final ConsentRequiuredStateRepository consentRequiuredStateRepository;

	private final ProviderStateConsentRepository providerStateConsentRepository;

	public ConsentDecisionServiceImpl(ConsentRequiuredStateRepository consentRequiuredStateRepository,
			ProviderStateConsentRepository providerStateConsentRepository) {
		this.consentRequiuredStateRepository = consentRequiuredStateRepository;
		this.providerStateConsentRepository = providerStateConsentRepository;
	}

	@Override
	public boolean determineAutoActivateCard(PaymentRequestDto paymentRequestDto) {
		String providerTin = extractProviderTin(paymentRequestDto);
		String providerState = extractProviderState(paymentRequestDto);

		if (!this.consentRequiuredStateRepository.existsByStateCodeAndConsentRequiredTrue(providerState)) {
			return true;
		}

		return this.providerStateConsentRepository.findByTaxIdAndStateCodeAndIsActiveTrue(providerTin, providerState)
			.isPresent();
	}

	private String extractProviderTin(PaymentRequestDto paymentRequestDto) {
		String providerTin = ((paymentRequestDto != null) && (paymentRequestDto.getPayeeDetails() != null))
				? paymentRequestDto.getPayeeDetails().getTaxId() : null;
		if (!StringUtils.hasText(providerTin)) {
			throw new IllegalArgumentException("Provider tax id is required for consent decision");
		}
		return providerTin.trim();
	}

	private String extractProviderState(PaymentRequestDto paymentRequestDto) {
		String providerState = ((paymentRequestDto != null) && (paymentRequestDto.getPayeeDetails() != null)
				&& (paymentRequestDto.getPayeeDetails().getAddress() != null))
						? paymentRequestDto.getPayeeDetails().getAddress().getState() : null;
		if (!StringUtils.hasText(providerState)) {
			throw new IllegalArgumentException("Provider state is required for consent decision");
		}
		return providerState.trim().toUpperCase(Locale.US);
	}

}
