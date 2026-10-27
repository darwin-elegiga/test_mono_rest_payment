package com.uhg.optum.financials.payments.payer.enums;

import com.fasterxml.jackson.annotation.JsonCreator;
import lombok.AllArgsConstructor;
import lombok.Getter;

import java.util.Arrays;

@AllArgsConstructor
@Getter
public enum OrganizationStatus {

	ACTIVE, CLOSED;

	@JsonCreator
	public static OrganizationStatus fromString(String name) {
		return Arrays.stream(OrganizationStatus.values())
			.filter((e) -> e.name().equalsIgnoreCase(name))
			.findFirst()
			.orElseThrow(IllegalArgumentException::new);
	}

}
