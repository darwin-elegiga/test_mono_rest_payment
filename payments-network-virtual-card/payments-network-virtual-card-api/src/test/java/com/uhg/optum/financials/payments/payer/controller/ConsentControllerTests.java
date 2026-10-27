package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.ConsentSaveResponseDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStateUpdateDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStatesDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStatesResponseDto;
import com.uhg.optum.financials.payments.payer.dto.ConsentStatesSaveRequestDto;
import com.uhg.optum.financials.payments.payer.service.ConsentService;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;

import java.util.List;

import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;

@ExtendWith(MockitoExtension.class)
class ConsentControllerTests {

	@InjectMocks
	private ConsentController consentController;

	@Mock
	private ConsentService consentService;

	@Test
	void getAllStatesConsent_returnsOkResponseWithStates() {
		ConsentStatesResponseDto expectedResponse = ConsentStatesResponseDto.builder()
			.states(List.of(
					ConsentStatesDto.builder().stateCode("CA").stateName("California").consentRequired(true).build(),
					ConsentStatesDto.builder().stateCode("TX").stateName("Texas").consentRequired(false).build()))
			.total(2)
			.build();

		given(this.consentService.getAllStatesConsent()).willReturn(expectedResponse);

		var response = this.consentController.getAllStatesConsent();

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(expectedResponse);
		then(this.consentService).should().getAllStatesConsent();
	}

	@Test
	void getAllStatesConsent_propagatesServiceException() {
		given(this.consentService.getAllStatesConsent()).willThrow(new IllegalStateException("Service unavailable"));

		Assertions.assertThatThrownBy(() -> this.consentController.getAllStatesConsent())
			.isInstanceOf(IllegalStateException.class)
			.hasMessageContaining("Service unavailable");
	}

	@Test
	void saveStatesConsent_returnsOkResponseWithMessageAndStates() {
		ConsentStatesSaveRequestDto request = ConsentStatesSaveRequestDto.builder()
			.states(List.of(ConsentStateUpdateDto.builder().stateCode("CA").consentRequired(true).build(),
					ConsentStateUpdateDto.builder().stateCode("TX").consentRequired(false).build(),
					ConsentStateUpdateDto.builder().stateCode("FL").consentRequired(true).build()))
			.build();

		ConsentSaveResponseDto expectedResponse = ConsentSaveResponseDto.builder()
			.message("Consent-required states updated successfully")
			.build();

		given(this.consentService.saveStatesConsent(request)).willReturn(expectedResponse);

		var response = this.consentController.saveStatesConsent(request);

		Assertions.assertThat(response.getBody()).isEqualTo(expectedResponse);
	}

	@Test
	void saveStatesConsent_propagatesServiceException() {
		ConsentStatesSaveRequestDto request = ConsentStatesSaveRequestDto.builder()
			.states(List.of(ConsentStateUpdateDto.builder().stateCode("CA").consentRequired(true).build()))
			.build();

		given(this.consentService.saveStatesConsent(request)).willThrow(new IllegalStateException("Save failed"));

		Assertions.assertThatThrownBy(() -> this.consentController.saveStatesConsent(request))
			.isInstanceOf(IllegalStateException.class)
			.hasMessageContaining("Save failed");
	}

}
