package com.uhg.optum.financials.payments.payer.constant;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;

class FileTypeTests {

	@Test
	void fileTypeEnum_shouldContainExpectedValues() {
		assertThat(this.getPayment835()).isEqualTo(FileType.PAYMENT835);
		assertThat(this.getCompanion()).isEqualTo(FileType.COMPANION);
		assertThat(FileType.values()).containsExactly(FileType.PAYMENT835, FileType.COMPANION);
	}

	private FileType getPayment835() {
		return FileType.PAYMENT835;
	}

	private FileType getCompanion() {
		return FileType.COMPANION;
	}

}
