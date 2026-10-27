package com.uhg.optum.financials.payments.payer.dto.document;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.UUID;

import org.assertj.core.api.Assertions;

@ExtendWith(MockitoExtension.class)
public class DocumentRequestTests {

	@InjectMocks
	DocumentRequest documentRequest;

	@Test
	void testDocumentRequestGettersAndSetters() {

		String key = "testKey";
		String docType = "PDF";
		UUID identifier = UUID.randomUUID();
		Boolean isPCI = true;
		MetaData metadata = new MetaData();
		Object templateMappedValues = new Object();

		this.documentRequest.setKey(key);
		this.documentRequest.setDocType(docType);
		this.documentRequest.setIdentifier(identifier);
		this.documentRequest.setIsPCI(isPCI);
		this.documentRequest.setMetadata(metadata);
		this.documentRequest.setTemplateMappedValues(templateMappedValues);

		Assertions.assertThat(this.documentRequest.getKey()).isNotNull();
		Assertions.assertThat(this.documentRequest.getDocType()).isNotNull();
		Assertions.assertThat(this.documentRequest.getIdentifier()).isNotNull();
		Assertions.assertThat(this.documentRequest.getIsPCI()).isNotNull();
		Assertions.assertThat(this.documentRequest.getMetadata()).isNotNull();
		Assertions.assertThat(this.documentRequest.getTemplateMappedValues()).isNotNull();
	}

	@Test
	void testCopyOf() {
		DocumentRequest source = new DocumentRequest();
		source.setKey("testKey");
		source.setDocType("vcp");
		source.setIdentifier(UUID.randomUUID());
		source.setIsPCI(true);
		source.setMetadata(new MetaData());
		source.setTemplateMappedValues(new Object());

		DocumentRequest copied = DocumentRequest.copyOf(source);

		Assertions.assertThat(copied).isNotNull();
		Assertions.assertThat(copied.getKey()).isEqualTo(source.getKey());
		Assertions.assertThat(copied.getDocType()).isEqualTo(source.getDocType());
		Assertions.assertThat(copied.getIdentifier()).isEqualTo(source.getIdentifier());
		Assertions.assertThat(copied.getIsPCI()).isEqualTo(source.getIsPCI());
		Assertions.assertThat(copied.getMetadata()).isEqualTo(source.getMetadata());
		Assertions.assertThat(copied.getTemplateMappedValues()).isEqualTo(source.getTemplateMappedValues());
	}

}
