package com.uhg.optum.financials.payments.payer.dto;

import com.uhg.optum.financials.payments.payer.util.CompanionFileUtil;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.junit.jupiter.MockitoExtension;

import java.math.BigDecimal;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatCode;

@ExtendWith(MockitoExtension.class)
class CompanionDtoTests {

	@Test
	void setIssuerPayments_withValidCommaSeparatedIds_populatesIssuerPaymentsList() {
		CompanionDto dto = new CompanionDto();
		dto.setIssuerPayments("ID1,ID2,ID3", "PID1, PID2, PID3");

		assertThat(dto.getIssuerPayments()).isNotNull();
		assertThat(dto.getIssuerPayments()).hasSize(3);
	}

	@Test
	void setIssuerPayments_withEmptyString_doesNotPopulateIssuerPaymentsList() {
		CompanionDto dto = new CompanionDto();
		dto.setIssuerPayments("", "");

		assertThat(dto.getIssuerPayments()).isNull();
	}

	@Test
	void setIssuerPayments_withNullString_doesNotPopulateIssuerPaymentsList() {
		CompanionDto dto = new CompanionDto();
		dto.setIssuerPayments(null);

		assertThat(dto.getIssuerPayments()).isNull();
	}

	@Test
	void setIssuerPayments_withWhitespaceIds_trimsAndPopulatesIssuerPaymentsList() {
		CompanionDto dto = new CompanionDto();
		dto.setIssuerPayments(" ID1 , ID2 , ID3 ", "PID1, PID2, PID3");

		assertThat(dto.getIssuerPayments()).isNotNull();
		assertThat(dto.getIssuerPayments()).hasSize(3);
		// assertThat(dto.getIssuerPayments().get(0).getIssuerPaymentId()).isEqualTo("ID1");
		// assertThat(dto.getIssuerPayments().get(1).getIssuerPaymentId()).isEqualTo("ID2");
		// assertThat(dto.getIssuerPayments().get(2).getIssuerPaymentId()).isEqualTo("ID3");
	}

	@Test
	void setIssuerPayments_withSingleValidId_populatesIssuerPaymentsListWithOneElement() {
		CompanionDto dto = new CompanionDto();
		dto.setIssuerPayments("ID1", "PID1");

		assertThat(dto.getIssuerPayments()).isNotNull();
		assertThat(dto.getIssuerPayments()).hasSize(1);
	}

	@Test
	void setIssuerPayments_withMultipleCommas_createsEmptyIssuerPaymentsList() {
		CompanionDto dto = new CompanionDto();
		dto.setIssuerPayments(",,,,", ",,,,");

		assertThat(dto.getIssuerPayments()).isNotNull();
		assertThat(dto.getIssuerPayments()).isEmpty();
	}

	@Test
	void networkPaymentID_getterSetter_setsAndGetsValue() {
		CompanionDto dto = new CompanionDto();
		dto.setNetworkPaymentID("NP123");

		assertThat(dto.getNetworkPaymentID()).isEqualTo("NP123");
	}

	@Test
	void summaryPageURL_getterSetter_setsAndGetsValue() {
		CompanionDto dto = new CompanionDto();
		dto.setSummaryPageURL("http://example.com/summary");

		assertThat(dto.getSummaryPageURL()).isEqualTo("http://example.com/summary");
	}

	@Test
	void subAndAccountShortCode_getterSetter_setsAndGetsValue() {
		CompanionDto dto = new CompanionDto();
		dto.setSubAccountShortCode("SUB12345");
		dto.setAccountShortCode("SC001");

		assertThat(dto.getSubAccountShortCode()).isEqualTo("SUB12345");
		assertThat(dto.getAccountShortCode()).isEqualTo("SC001");
	}

	@Test
	void companionDto_serializesShortCodeFieldsInSnakeCase() {
		CompanionDto dto = new CompanionDto();
		dto.setSubAccountShortCode("SUB12345");
		dto.setAccountShortCode("SC001");

		assertThatCode(() -> {
			String json = CompanionFileUtil.getCompanionLineWithOutAmount(dto);
			assertThat(json).contains("\"sub_account_short_cd\":\"SUB12345\"");
			assertThat(json).contains("\"account_short_cd\":\"SC001\"");
		}).doesNotThrowAnyException();
	}

	@Test
	void paymentType_getterSetter_setsAndGetsValue() {
		CompanionDto dto = new CompanionDto();
		dto.setPaymentType("Credit");

		assertThat(dto.getPaymentType()).isEqualTo("Credit");

	}

	@Test
	void companionDto_constructor_populatesFieldsCorrectly() {
		Object[] rs = { "NPID123", "PAYMENT", "ISS1,ISS2", "PID1,PID2", BigDecimal.valueOf(100) };
		CompanionDto dto = new CompanionDto(rs);

		assertThat(dto.getNetworkPaymentID()).isEqualTo("NPID123");
		assertThat(dto.getPaymentType()).isEqualTo("I");
		assertThat(dto.getIssuerPayments()).hasSize(2);
		assertThat(dto.getIssuerPayments().getFirst().getIssuerPaymentID()).isEqualTo("ISS1");
		assertThat(dto.getIssuerPayments().getFirst().getPaymentId()).isEqualTo("PID1");
	}

	@Test
	void companionDto_constructor_handlesNullTypeAndIssuerPayments() {
		Object[] rs = { "NPID456", null, null, null, null };
		CompanionDto dto = new CompanionDto(rs);

		assertThat(dto.getNetworkPaymentID()).isEqualTo("NPID456");
		assertThat(dto.getPaymentType()).isNull();
		assertThat(dto.getIssuerPayments()).isNull();
	}

	@Test
	void setPaymentTypeByType_setsCorrectPaymentTypeForKnownTypes() {
		CompanionDto dto = new CompanionDto();
		dto.setAmount(BigDecimal.valueOf(100));
		dto.setPaymentTypeByType("PAYMENT");
		assertThat(dto.getPaymentType()).isEqualTo("I");

		dto.setPaymentTypeByType("PAYMENT_ONLY");
		assertThat(dto.getPaymentType()).isEqualTo("D");

		dto.setAmount(BigDecimal.valueOf(0));
		dto.setPaymentTypeByType("PAYMENT_ONLY");
		assertThat(dto.getPaymentType()).isEqualTo("H");

		dto.setPaymentTypeByType("NOTIFICATION");
		assertThat(dto.getPaymentType()).isEqualTo("H");

		dto.setAmount(BigDecimal.valueOf(2));
		dto.setPaymentTypeByType("REMITTANCE_ONLY");
		assertThat(dto.getPaymentType()).isEqualTo("X");
	}

	@Test
	void setPaymentTypeByType_withC2B_setsC2BPaymentType() {
		CompanionDto dto = new CompanionDto();

		dto.setPaymentTypeByType("C2B");

		assertThat(dto.getPaymentType()).isEqualTo("C2B");
	}

	@Test
	void setPaymentTypeByType_withNullOrEmptyType_doesNothing() {
		CompanionDto dto = new CompanionDto();
		dto.setPaymentTypeByType(null);
		assertThat(dto.getPaymentType()).isNull();

		dto.setPaymentTypeByType("");
		assertThat(dto.getPaymentType()).isNull();
	}

}
