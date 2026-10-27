package com.uhg.optum.financials.payments.payer.dto;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;

class AuditLogDtoTests {

	@Test
	@DisplayName("All getters and setters work as expected for AuditLogDto")
	void auditLogDto_gettersAndSetters_shouldWorkCorrectly() {
		// given
		AuditLogDto thisAuditLogDto = new AuditLogDto();
		Long thisId = 123L;
		String thisPaymentId = "payment-456";
		UUID thisCardGuid = UUID.randomUUID();
		UUID thisRequestId = UUID.randomUUID();
		String thisReceiptId = "receipt-789";
		String thisEventType = "CREATE";
		String thisStatus = "SUCCESS";
		String thisSource = "SYSTEM";
		BigDecimal thisAmount = new BigDecimal("100.50");
		LocalDateTime thisCreatedAt = LocalDateTime.now();
		String thisCreatedBy = "user-abc";

		// when
		thisAuditLogDto.setId(thisId);
		thisAuditLogDto.setPaymentId(thisPaymentId);
		thisAuditLogDto.setCardGuid(thisCardGuid);
		thisAuditLogDto.setRequestId(thisRequestId);
		thisAuditLogDto.setReceiptId(thisReceiptId);
		thisAuditLogDto.setEventType(thisEventType);
		thisAuditLogDto.setStatus(thisStatus);
		thisAuditLogDto.setSource(thisSource);
		thisAuditLogDto.setAmount(thisAmount);
		thisAuditLogDto.setCreatedAt(thisCreatedAt);
		thisAuditLogDto.setCreatedBy(thisCreatedBy);

		// then
		assertThat(thisAuditLogDto.getId()).isEqualTo(thisId);
		assertThat(thisAuditLogDto.getPaymentId()).isEqualTo(thisPaymentId);
		assertThat(thisAuditLogDto.getCardGuid()).isEqualTo(thisCardGuid);
		assertThat(thisAuditLogDto.getRequestId()).isEqualTo(thisRequestId);
		assertThat(thisAuditLogDto.getReceiptId()).isEqualTo(thisReceiptId);
		assertThat(thisAuditLogDto.getEventType()).isEqualTo(thisEventType);
		assertThat(thisAuditLogDto.getStatus()).isEqualTo(thisStatus);
		assertThat(thisAuditLogDto.getSource()).isEqualTo(thisSource);
		assertThat(thisAuditLogDto.getAmount()).isEqualByComparingTo(thisAmount);
		assertThat(thisAuditLogDto.getCreatedAt()).isEqualTo(thisCreatedAt);
		assertThat(thisAuditLogDto.getCreatedBy()).isEqualTo(thisCreatedBy);
	}

}
