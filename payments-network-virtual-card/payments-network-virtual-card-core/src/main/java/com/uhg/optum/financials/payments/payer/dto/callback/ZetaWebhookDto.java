package com.uhg.optum.financials.payments.payer.dto.callback;

import com.fasterxml.jackson.annotation.JsonProperty;

import java.io.Serializable;
import java.time.Instant;

public record ZetaWebhookDto<T>(@JsonProperty("id") String id, @JsonProperty("specversion") String specversion,
		@JsonProperty("time") Instant time, @JsonProperty("status") String status, @JsonProperty("type") String type,
		@JsonProperty("data") T data, @JsonProperty("error") T error

) implements Serializable {
	private static final long serialVersionUID = 1L;

}
