package com.uhg.optum.financials.payments.payer.dto.document;

import lombok.Data;

import java.util.UUID;

@Data
public class DocumentRequest {

	private String key;

	private String docType;

	private UUID identifier;

	private Boolean isPCI;

	private MetaData metadata;

	private Object templateMappedValues;

	public static DocumentRequest copyOf(DocumentRequest source) {
		DocumentRequest target = new DocumentRequest();
		target.setKey(source.getKey());
		target.setDocType(source.getDocType());
		target.setIdentifier(source.getIdentifier());
		target.setIsPCI(source.getIsPCI());
		target.setMetadata(source.getMetadata());
		target.setTemplateMappedValues(source.getTemplateMappedValues());
		return target;
	}

}
