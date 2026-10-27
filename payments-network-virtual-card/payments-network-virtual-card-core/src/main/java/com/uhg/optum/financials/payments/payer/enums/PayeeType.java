package com.uhg.optum.financials.payments.payer.enums;

public enum PayeeType {

	C2B("C", "CB"), B2B("B", "BB");

	private final String inputCode;

	private final String prefix;

	PayeeType(String inputCode, String prefix) {
		this.inputCode = inputCode;
		this.prefix = prefix;
	}

	public String getInputCode() {
		return this.inputCode;
	}

	public String getPrefix() {
		return this.prefix;
	}

	public static PayeeType fromInput(String input) {
		if (input == null || input.isBlank()) {
			throw new IllegalArgumentException("Input cannot be null or blank. Expected B/B2B/BB or C/C2B/CB.");
		}

		String normalizedInput = input.trim().toUpperCase();
		return switch (normalizedInput) {
			case "B", "B2B", "BB" -> B2B;
			case "C", "C2B", "CB" -> C2B;
			default ->
				throw new IllegalArgumentException("Invalid input: " + input + ". Expected B/B2B/BB or C/C2B/CB.");
		};
	}

	public static String prefixFromInput(String input) {
		return fromInput(input).getPrefix();
	}

}
