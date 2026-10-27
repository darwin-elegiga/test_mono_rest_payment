package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.shared.client.constant.PaymentType;
import com.uhg.optum.financials.shared.client.dto.payment.AddressDto;
import com.uhg.optum.financials.shared.client.dto.payment.PayeeDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.client.dto.payment.ShippingDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.ClaimsDto;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.util.Map;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class PaymentRequestEntityUtilTests {

	@Test
	@DisplayName("toPaymentRequestEntity maps all fields from PaymentRequestDto")
	void toPaymentRequestEntity_shouldMapAllFields() {
		// given
		PaymentRequestDto thisDto = new PaymentRequestDto();
		thisDto.setOrigin("origin");
		thisDto.setOrgShortCode("org");
		thisDto.setAccountShortCode("acc");
		thisDto.setSubAccountShortCode("subacc");
		thisDto.setCorrelationId("corrId");
		PaymentDetailsDto thisPaymentDetailsDto = new PaymentDetailsDto();
		thisPaymentDetailsDto.setId("pdid");
		thisPaymentDetailsDto.setIssuerPaymentId("issuerId");
		thisPaymentDetailsDto.setReferenceId("refId");
		thisPaymentDetailsDto.setPaymentDate("2025-11-26");
		thisPaymentDetailsDto.setAdjudicationCutoffDate("2025-11-27");
		thisPaymentDetailsDto.setAdjudicationVersion("v1");
		thisPaymentDetailsDto.setAmount(BigDecimal.valueOf(123.45));
		thisPaymentDetailsDto.setRemitMethod("method");
		thisPaymentDetailsDto.setJurisdictionalBody("body");
		thisPaymentDetailsDto.setNumberOfAttachedPdfs(2);
		thisPaymentDetailsDto.setSettlementTime("12:00");
		thisPaymentDetailsDto.setType(PaymentType.valueOf("PAYMENT"));
		thisDto.setPaymentDetails(thisPaymentDetailsDto);
		PayeeDetailsDto thisPayeeDetailsDto = new PayeeDetailsDto();
		thisPayeeDetailsDto.setType("type");
		thisPayeeDetailsDto.setName("name");
		AddressDto thisAddressDto = new AddressDto();
		thisAddressDto.setLine1("line1");
		thisAddressDto.setLine2("line2");
		thisAddressDto.setCity("city");
		thisAddressDto.setState("state");
		thisAddressDto.setCountry("country");
		thisAddressDto.setPostalCode("12345");
		thisPayeeDetailsDto.setAddress(thisAddressDto);
		thisPayeeDetailsDto.setTaxId("taxId");
		thisPayeeDetailsDto.setNpi("npi");
		thisPayeeDetailsDto.setReceiverId("receiverId");
		thisDto.setPayeeDetails(thisPayeeDetailsDto);
		ShippingDetailsDto thisShippingDetailsDto = new ShippingDetailsDto();
		thisShippingDetailsDto.setName("shipName");
		thisShippingDetailsDto.setAddress(thisAddressDto);
		thisDto.setShippingDetails(thisShippingDetailsDto);
		ClaimsDto thisClaimsDto = new ClaimsDto();
		thisClaimsDto.setTotalCount(5);
		thisClaimsDto.setUrl("url");
		thisClaimsDto.setDocType("docType");
		thisClaimsDto.setPlb(true);
		thisDto.setClaims(thisClaimsDto);
		thisDto.setMetadata(Map.of("key", "value"));
		thisDto.setRePreferenced(true);

		// when
		PaymentRequest thisEntity = PaymentRequestEntityUtil.toPaymentRequestEntity(thisDto);

		// then
		assertThat(thisEntity.getOrigin()).isEqualTo("origin");
		assertThat(thisEntity.getOrgShortCode()).isEqualTo("org");
		assertThat(thisEntity.getAccountShortCode()).isEqualTo("acc");
		assertThat(thisEntity.getSubAccountShortCode()).isEqualTo("subacc");
		assertThat(thisEntity.getCorrelationId()).isEqualTo("corrId");
		assertThat(thisEntity.getPaymentDetails()).isNotNull();
		assertThat(thisEntity.getPaymentDetails().getId()).isEqualTo("pdid");
		assertThat(thisEntity.getPaymentDetails().getIssuerPaymentId()).isEqualTo("issuerId");
		assertThat(thisEntity.getPaymentDetails().getReferenceId()).isEqualTo("refId");
		assertThat(thisEntity.getPaymentDetails().getPaymentDate()).isNotNull();
		assertThat(thisEntity.getPaymentDetails().getAdjudicationCutoffDate()).isNotNull();
		assertThat(thisEntity.getPaymentDetails().getAdjudicationVersion()).isEqualTo("v1");
		assertThat(thisEntity.getPaymentDetails().getAmount()).isEqualTo(new BigDecimal("123.45"));
		assertThat(thisEntity.getPaymentDetails().getRemitMethod()).isEqualTo("method");
		assertThat(thisEntity.getPaymentDetails().getJurisdictionalBody()).isEqualTo("body");
		assertThat(thisEntity.getPaymentDetails().getNumberOfAttachedPdfs()).isEqualTo(2);
		assertThat(thisEntity.getPaymentDetails().getSettlementTime()).isEqualTo("12:00");
		assertThat(thisEntity.getPaymentDetails().getType()).isEqualTo(PaymentType.PAYMENT);
		assertThat(thisEntity.getPaymentDetails().getNetworkPaymentId()).isNull();
		assertThat(thisEntity.getPayeeDetails()).isNotNull();
		assertThat(thisEntity.getPayeeDetails().getType()).isEqualTo("type");
		assertThat(thisEntity.getPayeeDetails().getName()).isEqualTo("name");
		assertThat(thisEntity.getPayeeDetails().getAddress()).isNotNull();
		assertThat(thisEntity.getPayeeDetails().getAddress().getLine1()).isEqualTo("line1");
		assertThat(thisEntity.getPayeeDetails().getTaxId()).isEqualTo("taxId");
		assertThat(thisEntity.getPayeeDetails().getNpi()).isEqualTo("npi");
		assertThat(thisEntity.getPayeeDetails().getReceiverId()).isEqualTo("receiverId");
		assertThat(thisEntity.getShippingDetails()).isNotNull();
		assertThat(thisEntity.getShippingDetails().getName()).isEqualTo("shipName");
		assertThat(thisEntity.getShippingDetails().getAddress()).isNotNull();
		assertThat(thisEntity.getTotalClmCount()).isEqualTo(5);
		assertThat(thisEntity.getClmUrl()).isEqualTo("url");
		assertThat(thisEntity.getDocType()).isEqualTo("docType");
		assertThat(thisEntity.isPlb()).isTrue();
		assertThat(thisEntity.getMetadata()).containsEntry("key", "value");
		assertThat(thisEntity.isRePreferenceFlag()).isTrue();
		assertThat(thisEntity.isRePreferenceProcessed()).isFalse();
	}

	@Test
	@DisplayName("checkNullShippingDtoAddress returns true for all blank/null fields")
	void checkNullShippingDtoAddress_shouldReturnTrueForAllBlankFields() {
		// given
		AddressDto thisAddressDto = new AddressDto();
		// when
		boolean thisResult = PaymentRequestEntityUtil.checkNullShippingDtoAddress(thisAddressDto);
		// then
		assertThat(thisResult).isTrue();
	}

	@Test
	@DisplayName("checkNullShippingDtoAddress returns false for non-blank fields")
	void checkNullShippingDtoAddress_shouldReturnFalseForNonBlankFields() {
		// given
		AddressDto thisAddressDto = new AddressDto();
		thisAddressDto.setLine1("line1");
		// when
		boolean thisResult = PaymentRequestEntityUtil.checkNullShippingDtoAddress(thisAddressDto);
		// then
		assertThat(thisResult).isFalse();
	}

	@Test
	@DisplayName("generateNetworkPaymentId should return BB prefix with 9-char base36 suffix for B2B")
	void generateNetworkPaymentId_shouldReturnB2BPrefix() {
		assertThat(PaymentRequestEntityUtil.generateNetworkPaymentId("B2B", 2_821_109_907_456L))
			.isEqualTo("BB100000000");
	}

	@Test
	@DisplayName("generateNetworkPaymentId should return CB prefix with 9-char base36 suffix for C2B")
	void generateNetworkPaymentId_shouldReturnC2BPrefix() {
		assertThat(PaymentRequestEntityUtil.generateNetworkPaymentId("C", 2_821_109_907_457L)).isEqualTo("CB100000001");
	}

	@Test
	@DisplayName("generateNetworkPaymentId should reject sequence values below base36 start range")
	void generateNetworkPaymentId_shouldRejectOutOfRange() {
		assertThatThrownBy(() -> PaymentRequestEntityUtil.generateNetworkPaymentId("B", 2_821_109_907_455L))
			.isInstanceOf(IllegalArgumentException.class);
	}

	@Test
	@DisplayName("resolveRePreferenceFlag returns true when dto has rePreferenced=true")
	void resolveRePreferenceFlag_shouldReturnDtoBooleanValue() {
		PaymentRequestDto dto = new PaymentRequestDto();
		dto.setRePreferenced(true);
		assertThat(PaymentRequestEntityUtil.resolveRePreferenceFlag(dto)).isTrue();

		dto.setRePreferenced(false);
		assertThat(PaymentRequestEntityUtil.resolveRePreferenceFlag(dto)).isFalse();

		assertThat(PaymentRequestEntityUtil.resolveRePreferenceFlag(null)).isFalse();
	}

	@Test
	@DisplayName("resolveRePreferenceFlag returns false when rePreferenced is null (Boolean type)")
	void resolveRePreferenceFlag_shouldReturnFalseWhenNullableBoolean() {
		PaymentRequestDto dto = new PaymentRequestDto();
		dto.setRePreferenced(null);
		assertThat(PaymentRequestEntityUtil.resolveRePreferenceFlag(dto)).isFalse();
	}

}
