package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.ConsentStateUpdateDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStatesDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStatesResponseDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStatesSaveRequestDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentSaveResponseDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.model.ConsentRequiredStates;
import com.uhg.optum.financials.payments.payer.repository.ConsentRequiredStateRepository;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;

import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;

@ExtendWith(MockitoExtension.class)
public class ConsentServiceImplTests {

	@Mock
	private ConsentRequiredStateRepository consentRequiredStateRepository;

	@InjectMocks
	private ConsentServiceImpl consentService;

	@Test
	void getAllStatesConsent_mapsRepositoryStatesToResponse() {
		ConsentRequiredStates california = new ConsentRequiredStates();
		california.setStateCode("CA");
		california.setStateName("California");
		california.setConsentRequired(true);

		ConsentRequiredStates texas = new ConsentRequiredStates();
		texas.setStateCode("TX");
		texas.setStateName("Texas");
		texas.setConsentRequired(false);

		given(this.consentRequiredStateRepository.findAllByOrderByStateNameAsc())
			.willReturn(List.of(california, texas));

		ConsentStatesResponseDto response = this.consentService.getAllStatesConsent();

		Assertions.assertThat(response).isNotNull();
		Assertions.assertThat(response.getTotal()).isEqualTo(2);
		Assertions.assertThat(response.getStates()).hasSize(2);

		ConsentStatesDto firstState = response.getStates().getFirst();
		Assertions.assertThat(firstState.getStateCode()).isEqualTo("CA");
		Assertions.assertThat(firstState.getStateName()).isEqualTo("California");
		Assertions.assertThat(firstState.isConsentRequired()).isTrue();

		ConsentStatesDto secondState = response.getStates().get(1);
		Assertions.assertThat(secondState.getStateCode()).isEqualTo("TX");
		Assertions.assertThat(secondState.getStateName()).isEqualTo("Texas");
		Assertions.assertThat(secondState.isConsentRequired()).isFalse();

		then(this.consentRequiredStateRepository).should().findAllByOrderByStateNameAsc();
	}

	@Test
	void getAllStatesConsent_returnsEmptyStatesWhenRepositoryReturnsNone() {
		given(this.consentRequiredStateRepository.findAllByOrderByStateNameAsc()).willReturn(List.of());

		ConsentStatesResponseDto response = this.consentService.getAllStatesConsent();

		Assertions.assertThat(response).isNotNull();
		Assertions.assertThat(response.getStates()).isEmpty();
		Assertions.assertThat(response.getTotal()).isZero();
		then(this.consentRequiredStateRepository).should().findAllByOrderByStateNameAsc();
	}

	@Test
	void getAllStatesConsent_returnsConfiguredCountForStatesSet() {
		ConsentRequiredStates alabama = new ConsentRequiredStates();
		alabama.setStateCode("AL");
		alabama.setStateName("Alabama");
		alabama.setConsentRequired(false);

		ConsentRequiredStates wyoming = new ConsentRequiredStates();
		wyoming.setStateCode("WY");
		wyoming.setStateName("Wyoming");
		wyoming.setConsentRequired(false);

		given(this.consentRequiredStateRepository.findAllByOrderByStateNameAsc()).willReturn(List.of(alabama, wyoming));

		ConsentStatesResponseDto response = this.consentService.getAllStatesConsent();

		Assertions.assertThat(response.getTotal()).isEqualTo(2);
		Assertions.assertThat(response.getStates())
			.extracting(ConsentStatesDto::getStateCode)
			.containsExactly("AL", "WY");
	}

	@Test
	void saveStatesConsent_returnsSuccessMessageAndUpdatedStates() {
		ConsentRequiredStates california = new ConsentRequiredStates();
		california.setId(1L);
		california.setStateCode("CA");
		california.setStateName("California");
		california.setConsentRequired(false);

		ConsentRequiredStates texas = new ConsentRequiredStates();
		texas.setId(2L);
		texas.setStateCode("TX");
		texas.setStateName("Texas");
		texas.setConsentRequired(true);

		ConsentRequiredStates florida = new ConsentRequiredStates();
		florida.setId(3L);
		florida.setStateCode("FL");
		florida.setStateName("Florida");
		florida.setConsentRequired(false);
		florida.setCreatedBy("seed-user");

		ConsentStatesSaveRequestDto request = ConsentStatesSaveRequestDto.builder()
			.states(List.of(ConsentStateUpdateDto.builder().stateCode("CA").consentRequired(true).build(),
					ConsentStateUpdateDto.builder().stateCode("TX").consentRequired(false).build(),
					ConsentStateUpdateDto.builder().stateCode("FL").consentRequired(true).build()))
			.build();

		given(this.consentRequiredStateRepository.findAllByOrderByStateNameAsc())
			.willReturn(List.of(california, texas, florida));
		given(this.consentRequiredStateRepository.saveAll(org.mockito.ArgumentMatchers.anyList()))
			.willAnswer((invocation) -> invocation.getArgument(0));

		ConsentSaveResponseDto response = this.consentService.saveStatesConsent(request);

		Assertions.assertThat(response).isNotNull();
		Assertions.assertThat(response.getMessage()).isEqualTo("Consent-required states updated successfully");
		Assertions.assertThat(california.isConsentRequired()).isTrue();
		Assertions.assertThat(texas.isConsentRequired()).isFalse();
		Assertions.assertThat(florida.isConsentRequired()).isTrue();
		Assertions.assertThat(california.getCreatedBy()).isEqualTo("anonymous");
		Assertions.assertThat(texas.getCreatedBy()).isEqualTo("anonymous");
		Assertions.assertThat(florida.getCreatedBy()).isEqualTo("seed-user");
		Assertions.assertThat(california.getUpdatedBy()).isEqualTo("anonymous");
		Assertions.assertThat(texas.getUpdatedBy()).isEqualTo("anonymous");
		Assertions.assertThat(florida.getUpdatedBy()).isEqualTo("anonymous");

	}

	@Test
	void saveStatesConsent_throwsBadRequestForUnknownStateCode() {
		ConsentStatesSaveRequestDto request = ConsentStatesSaveRequestDto.builder()
			.states(List.of(ConsentStateUpdateDto.builder().stateCode("XX").consentRequired(true).build()))
			.build();

		ConsentRequiredStates california = new ConsentRequiredStates();
		california.setStateCode("CA");
		california.setStateName("California");
		california.setConsentRequired(false);

		given(this.consentRequiredStateRepository.findAllByOrderByStateNameAsc()).willReturn(List.of(california));

		Assertions.assertThatThrownBy(() -> this.consentService.saveStatesConsent(request))
			.isInstanceOf(BadRequestException.class)
			.hasMessageContaining("Invalid states received: XX");
	}

	@Test
	void saveStatesConsent_returnsRequestedStateCodesInResponse() {
		ConsentRequiredStates california = new ConsentRequiredStates();
		california.setStateCode("CA");
		california.setStateName("California");
		california.setConsentRequired(false);

		ConsentStatesSaveRequestDto request = ConsentStatesSaveRequestDto.builder()
			.states(List.of(ConsentStateUpdateDto.builder().stateCode("CA").consentRequired(true).build()))
			.build();

		given(this.consentRequiredStateRepository.findAllByOrderByStateNameAsc()).willReturn(List.of(california));
		given(this.consentRequiredStateRepository.saveAll(org.mockito.ArgumentMatchers.anyList()))
			.willAnswer((invocation) -> invocation.getArgument(0));

		ConsentSaveResponseDto response = this.consentService.saveStatesConsent(request);

		then(this.consentRequiredStateRepository).should().findAllByOrderByStateNameAsc();
		Assertions.assertThat(response.getMessage()).isEqualTo("Consent-required states updated successfully");
	}

}
