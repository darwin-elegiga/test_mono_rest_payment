package com.uhg.optum.financials.payments.payer;

import org.springframework.boot.autoconfigure.SpringBootApplication;

@SpringBootApplication
public final class SpecGenerator {

	public static void main(String... args) {
		com.uhg.optum.financials.shared.api.spec.SpecGenerator.main(args);
	}

}
