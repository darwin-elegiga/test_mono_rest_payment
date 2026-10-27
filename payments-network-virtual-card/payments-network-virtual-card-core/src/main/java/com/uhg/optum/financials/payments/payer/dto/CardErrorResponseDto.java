package com.uhg.optum.financials.payments.payer.dto;

import lombok.Data;

import java.util.LinkedHashMap;

@Data
public class CardErrorResponseDto {

	private String code;

	private String message;

	private LinkedHashMap<String, Object> details;

}
