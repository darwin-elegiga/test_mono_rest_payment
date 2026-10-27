package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.ConsentSaveResponseDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStateUpdateDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStatesDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStatesSaveRequestDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStatesResponseDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.model.ConsentRequiredStates;
import com.uhg.optum.financials.payments.payer.repository.ConsentRequiredStateRepository;
import com.uhg.optum.financials.payments.payer.service.ConsentService;
import com.uhg.optum.financials.payments.payer.util.UserNameExtractUtil;
import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.Set;
import java.util.function.Function;
import java.util.stream.Collectors;

@Slf4j
@Service
@RequiredArgsConstructor
public class ConsentServiceImpl implements ConsentService {

	private static final String SUCCESS_MESSAGE = "Consent-required states updated successfully";

	private final ConsentRequiredStateRepository consentRequiredStateRepository;

	@Override
	public ConsentStatesResponseDto getAllStatesConsent() {
		log.info("Fetching all US states for consent details");

		List<ConsentRequiredStates> consentStates = this.consentRequiredStateRepository.findAllByOrderByStateNameAsc();
		List<ConsentStatesDto> stateDtos = consentStates.stream()
			.map((state) -> ConsentStatesDto.builder()
				.stateCode(state.getStateCode())
				.stateName(state.getStateName())
				.consentRequired(state.isConsentRequired())
				.build())
			.toList();

		return ConsentStatesResponseDto.builder().states(stateDtos).total(stateDtos.size()).build();
	}

	@Override
	@Transactional
	public ConsentSaveResponseDto saveStatesConsent(ConsentStatesSaveRequestDto request) {
		if (request == null || request.getStates() == null || request.getStates().isEmpty()) {
			throw new BadRequestException("States cannot be null or empty.");
		}
		String updatedByUser = UserNameExtractUtil.getUserNameFromToken();
		log.info("Saving consent-required states in bulk: {} records", request.getStates().size());
		List<ConsentRequiredStates> statesToUpdate = new ArrayList<>();
		List<ConsentRequiredStates> allStates = this.consentRequiredStateRepository.findAllByOrderByStateNameAsc();
		Map<String, ConsentRequiredStates> statesByCode = allStates.stream()
			.collect(Collectors.toMap(ConsentRequiredStates::getStateCode, Function.identity()));
		Set<String> invalidStateCodes = new LinkedHashSet<>();
		for (ConsentStateUpdateDto requestedState : request.getStates()) {
			String stateCode = requestedState.getStateCode();
			ConsentRequiredStates stateToSave = statesByCode.get(stateCode);
			if (stateToSave == null) {
				invalidStateCodes.add(stateCode);
				continue;
			}
			if (Objects.isNull(stateToSave.getCreatedBy()) || stateToSave.getCreatedBy().isBlank()) {
				stateToSave.setCreatedBy(updatedByUser);
			}
			stateToSave.setUpdatedBy(updatedByUser);
			stateToSave.setUpdatedAt(LocalDateTime.now());
			stateToSave.setConsentRequired(requestedState.getConsentRequired());
			statesToUpdate.add(stateToSave);
		}
		if (!invalidStateCodes.isEmpty()) {
			throw new BadRequestException("Invalid states received: " + String.join(", ", invalidStateCodes));
		}
		this.consentRequiredStateRepository.saveAll(statesToUpdate);
		return ConsentSaveResponseDto.builder().message(SUCCESS_MESSAGE).build();
	}

}
