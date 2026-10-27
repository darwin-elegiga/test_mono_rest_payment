package com.uhg.optum.financials.payments.payer.dto.document;

import org.assertj.core.api.Assertions;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.UUID;

@ExtendWith(MockitoExtension.class)
public class DocumentResponseTests {

	@InjectMocks
	DocumentResponse response;

	@Test
	void testGettersAndSetters() {

		UUID identifier = UUID.randomUUID();
		String documentUrl = "http://example.com/doc";
		String documentId = "doc123";
		String errorDesc = "No error";
		String documentType = "PDF";

		this.response.setIdentifier(identifier);
		this.response.setDocumentUrl(documentUrl);
		this.response.setDocumentId(documentId);
		this.response.setErrorDesc(errorDesc);
		this.response.setDocumentType(documentType);

		Assertions.assertThat(this.response.getIdentifier()).isNotNull();
		Assertions.assertThat(this.response.getDocumentUrl()).isNotNull();
		Assertions.assertThat(this.response.getDocumentId()).isNotNull();
		Assertions.assertThat(this.response.getErrorDesc()).isNotNull();
		Assertions.assertThat(this.response.getDocumentType()).isNotNull();

	}

}
