package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.DocumentResponseDetails;
import com.uhg.optum.financials.payments.payer.repository.DocumentResponseDetailsRepository;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

import java.util.Collections;
import java.util.List;

import static org.mockito.Mockito.times;

class DocumentResponseDetailsServiceImplTests {

	@Mock
	private DocumentResponseDetailsRepository documentResponseDetailsRepository;

	@InjectMocks
	private DocumentResponseDetailsServiceImpl documentResponseDetailsService;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	void getDocumentResponseDetails_returnsList_whenFound() {
		String issuerPaymentId = "123";
		DocumentResponseDetails details = new DocumentResponseDetails();
		List<DocumentResponseDetails> detailsList = List.of(details);

		BDDMockito.when(this.documentResponseDetailsRepository.findByNetworkPaymentId(issuerPaymentId))
			.thenReturn(detailsList);

		List<DocumentResponseDetails> result = this.documentResponseDetailsService
			.getDocumentResponseDetails(issuerPaymentId);

		Assertions.assertThat(result).isNotNull();
		BDDMockito.verify(this.documentResponseDetailsRepository, times(1)).findByNetworkPaymentId(issuerPaymentId);
	}

	@Test
	void getDocumentResponseDetails_throwsNotFoundException_whenListIsNull() {
		String issuerPaymentId = "123";
		BDDMockito.when(this.documentResponseDetailsRepository.findByNetworkPaymentId(issuerPaymentId))
			.thenReturn(null);

		Assertions
			.assertThatThrownBy(() -> this.documentResponseDetailsService.getDocumentResponseDetails(issuerPaymentId))
			.isInstanceOf(NotFoundException.class);
	}

	@Test
	void getDocumentResponseDetails_throwsNotFoundException_whenListIsEmpty() {
		String issuerPaymentId = "123";
		BDDMockito.when(this.documentResponseDetailsRepository.findByNetworkPaymentId(issuerPaymentId))
			.thenReturn(Collections.emptyList());

		Assertions
			.assertThatThrownBy(() -> this.documentResponseDetailsService.getDocumentResponseDetails(issuerPaymentId))
			.isInstanceOf(NotFoundException.class);
	}

}
