package com.uhg.optum.financials.payments.payer.model;

import org.assertj.core.api.Assertions;
import jakarta.persistence.Column;
import org.junit.jupiter.api.Test;

class ConsentRequiredStatesTests {

	@Test
	void consentRequiredStates_hasAutomaticTimestampAnnotations() throws NoSuchFieldException {
		var createdAtField = ConsentRequiredStates.class.getDeclaredField("createdAt");
		var updatedAtField = ConsentRequiredStates.class.getDeclaredField("updatedAt");
		var createdAtColumn = createdAtField.getAnnotation(Column.class);
		var updatedAtColumn = updatedAtField.getAnnotation(Column.class);

		Assertions.assertThat(createdAtColumn).isNotNull();
		Assertions.assertThat(createdAtColumn.nullable()).isFalse();
		Assertions.assertThat(createdAtColumn.columnDefinition()).contains("DEFAULT CURRENT_TIMESTAMP");

		Assertions.assertThat(updatedAtColumn).isNotNull();
		Assertions.assertThat(updatedAtColumn.nullable()).isFalse();
		Assertions.assertThat(updatedAtColumn.columnDefinition()).contains("DEFAULT CURRENT_TIMESTAMP");
	}

}
