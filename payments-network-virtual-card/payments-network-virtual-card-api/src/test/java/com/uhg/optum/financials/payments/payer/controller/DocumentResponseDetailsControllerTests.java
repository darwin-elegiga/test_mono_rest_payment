package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.model.DocumentResponseDetails;
import com.uhg.optum.financials.payments.payer.service.DocumentResponseDetailsService;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import org.springframework.http.ResponseEntity;

import java.util.Arrays;
import java.util.List;

class DocumentResponseDetailsControllerTests {

	@Mock
	private DocumentResponseDetailsService documentResponseDetailsService;

	@InjectMocks
	private DocumentResponseDetailsController controller;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	void getDocumentResponseDetails_returnsList() {
		String issuerPaymentId = "123";
		DocumentResponseDetails detail = new DocumentResponseDetails();
		List<DocumentResponseDetails> details = Arrays.asList(detail);

		BDDMockito.when(this.documentResponseDetailsService.getDocumentResponseDetails(issuerPaymentId))
			.thenReturn(details);

		ResponseEntity<List<DocumentResponseDetails>> response = this.controller
			.getDocumentResponseDetails(issuerPaymentId);
		Assertions.assertThat(response).isNotNull();
		Assertions.assertThat(response.getBody()).isEqualTo(details);
	}

}
