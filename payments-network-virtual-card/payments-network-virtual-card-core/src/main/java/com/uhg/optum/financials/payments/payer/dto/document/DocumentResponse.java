package com.uhg.optum.financials.payments.payer.dto.document;

import lombok.Getter;
import lombok.Setter;
import net.minidev.json.JSONObject;

import java.util.UUID;

@Getter
@Setter
public class DocumentResponse {

	@Getter
	@Setter
	private UUID identifier;

	@Getter
	@Setter
	private String documentUrl;

	@Getter
	@Setter
	private String documentId;

	@Getter
	@Setter
	private String errorDesc;

	@Getter
	@Setter
	private String documentType;

	@Getter
	@Setter
	private JSONObject metadata;

}
