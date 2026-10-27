package com.uhg.optum.financials.payments.features.paymentDtoValidation.test;

import com.uhg.optum.financials.payments.common.TestContext;
import com.uhg.optum.financials.payments.features.paymentDtoValidation.implementation.PaymentRequestDTOValidationImpl;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.core.constants.LevelNames;
import okhttp3.ResponseBody;
import org.assertj.core.api.Assertions;
import org.json.JSONException;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.parallel.Execution;
import org.junit.jupiter.api.parallel.ExecutionMode;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.NullAndEmptySource;
import org.junit.jupiter.params.provider.ValueSource;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ContextConfiguration;

import java.io.IOException;

@SpringBootTest
@ContextConfiguration(classes = PaymentRequestDTOValidationTests.class)
@Execution(ExecutionMode.CONCURRENT)
public class PaymentRequestDTOValidationTests extends PaymentRequestDTOValidationImpl {

	private static TestContext testContext;

	@BeforeAll
	static void setUp() throws IOException, JSONException {
		testContext = baseSetUp();
	}

	@Test
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	void testPaymentRequestDTO() throws IOException {
		PaymentRequestDto dto = getPaymentRequest(getOrgShortCode(), getAccountShortCode(), getSubAccountShortCode());
		apiEndPointsImpl.loadPayment(dto);
	}

	@Override
	protected String getOrgShortCode() {
		return testContext.orgShortCode();
	}

	@Override
	protected String getAccountShortCode() {
		return testContext.accountShortCode();
	}

	@Override
	protected String getSubAccountShortCode() {
		return testContext.subAccountShortCode();
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@NullAndEmptySource
	@ParameterizedTest
	void testPaymentRequestDTOWithCorrelationId(String correlationId) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidCorrelationId(correlationId);
		Assertions.assertThat(response.code()).isEqualTo(200);
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "empty", "null", "length" })
	void testPaymentRequestDTOWithOrigin(String originID) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidOrigin(originID);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		switch (originID) {
			case "null", "empty" -> compareContains("originID", "Origin cannot be null or empty.", body);
			case "length" -> compareContains("originID", "Origin must be between 1 and 50 characters.", body);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@NullAndEmptySource
	@ParameterizedTest
	void testPaymentRequestDTOWithOrgShortCode(String orgShortCodeParam) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidOrgShortCode(orgShortCodeParam);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		compareContains("orgShortCode", "Org Short Code cannot be null or empty.", body);
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@NullAndEmptySource
	@ParameterizedTest
	void testPaymentRequestDTOWithAccountShortCode(String accountShortCodeParam) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidAccountShortCode(accountShortCodeParam);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		compareContains("accountShortCode", "Account Short Code cannot be null or empty.", body);
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty" })
	void testPaymentRequestDTOWithSubAccountShortCode(String subAccountShortCodeParam) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidSubAccountShortCode(subAccountShortCodeParam);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		compareContains("subAccountShortCode", "Sub Account ShortCode cannot be null or empty.", body);
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty", "length" })
	void testPaymentRequestDTOWithPaymentId(String paymentId) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidPaymentId(paymentId);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		switch (paymentId) {
			case "null", "empty" -> compareContains("paymentId", "Payment Id cannot null or empty.", body);
			case "length" -> compareContains("paymentId", "Payment Id should be between 1 and 32 characters.", body);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty", "length" })
	void testPaymentRequestDTOWithIssuerPaymentId(String issuerPaymentId) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidIssuerPaymentId(issuerPaymentId);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		switch (issuerPaymentId) {
			case "null", "empty" -> compareContains("issuerPaymentId", "Issuer payment Id cannot null or empty.", body);
			case "length" ->
				compareContains("issuerPaymentId", "Issuer payment Id should be between 1 and 50 characters.", body);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "length", "null", "empty" })
	void testPaymentRequestDTOWithReferenceId(String referenceId) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidReferenceId(referenceId);

		switch (referenceId) {
			case "length" -> {
				Assertions.assertThat(response.code()).isEqualTo(400);
				String body = response.errorBody().string();
				compareContains("referenceId", "Payment Reference id should not be greater than 50 characters.", body);
			}
			case "null", "empty" -> Assertions.assertThat(response.code()).isEqualTo(200);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty", "invalid" })
	void testPaymentRequestDTOWithPaymentDate(String paymentDate) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidPaymentDate(paymentDate);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		switch (paymentDate) {
			case "null" -> compareContains("paymentDate", "PaymentDate should not be null", body);
			case "empty" -> compareContains("paymentDate",
					"PaymentDate should not be empty and date should be in format yyyy-MM-dd", body);
			case "invalid" -> compareContains("paymentDate", "Payment Date field should be in yyyy-MM-dd format", body);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "invalid", "null", "empty" })
	void testPaymentRequestDTOWithAdjudicationCutoffDate(String paymentDate) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidAdjudicationCutoffDate(paymentDate);
		switch (paymentDate) {
			case "invalid" -> {
				Assertions.assertThat(response.code()).isEqualTo(400);
				String body = response.errorBody().string();
				compareContains("paymentDate", "Adjudication Date field should be in yyyy-MM-dd format", body);
			}
			case "null", "empty" -> Assertions.assertThat(response.code()).isEqualTo(200);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "length", "null", "empty" })
	void testPaymentRequestDTOWithAdjudicationVersion(String adjudicationVersion) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidAdjudicationVersion(adjudicationVersion);

		switch (adjudicationVersion) {
			case "length" -> {
				Assertions.assertThat(response.code()).isEqualTo(400);
				String body = response.errorBody().string();
				compareContains("adjudicationVersion",
						"Payment Adjudication Version should not be greater than 50 characters.", body);
			}
			case "null", "empty" -> Assertions.assertThat(response.code()).isEqualTo(200);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "100M", "decimal", "negative" })
	void testPaymentRequestDTOWithAmount(String amount) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidAmount(amount);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		switch (amount) {
			case "null" -> compareContains("amount", "Amount should not be null", body);
			case "100M" -> compareContains("amount", "should be less than 100000000", body);
			case "decimal" -> compareContains("amount", "Amount should have 2 precision values", body);
			case "negative" -> compareContains("amount", "Amount should be a non-negative integer", body);
		}
	}

	@Tag("eph")
	@ParameterizedTest
	@ValueSource(doubles = { 0.00 })
	void testPaymentRequestDTOWithZeroAmount(double amount) throws IOException {
		retrofit2.Response<ResponseBody> response = setAmount(amount);
		Assertions.assertThat(response.code()).isEqualTo(200);
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty", "invalid", "PDF", "DATA" })
	void testPaymentRequestDTOWithRemitMethod(String remitMethod) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidRemitMethod(remitMethod);
		if (remitMethod.equals("PDF") || remitMethod.equals("DATA")) {
			Assertions.assertThat(response.code()).isEqualTo(200);
		}
		else {
			Assertions.assertThat(response.code()).isEqualTo(400);
			String body = response.errorBody().string();
			compareContains("remitMethod", "Remit Method should be in [PDF, DATA, TEXT, NONE]", body);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "size", "zero" })
	void testPaymentRequestDTOWithNumberOfAttachedPdfs(String numberOfAttachedPdfs) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidNumberOfAttachedPdfs(numberOfAttachedPdfs);
		if ("size".equals(numberOfAttachedPdfs)) {
			Assertions.assertThat(response.code()).isEqualTo(400);
			String body = response.errorBody().string();
			compareContains("numberOfAttachedPdfs", "Number Of Attached Pdfs should be less than or equal to 9999",
					body);
		}
		else if ("zero".equals(numberOfAttachedPdfs)) {
			Assertions.assertThat(response.code()).isEqualTo(200);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty", "invalid", "000000", "000001", "000002", "000003" })
	void testPaymentRequestDTOWithSettlementTime(String settlementTime) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidSettlementTime(settlementTime);

		switch (settlementTime) {
			case "null", "empty", "invalid" -> {
				Assertions.assertThat(response.code()).isEqualTo(400);
				String body = response.errorBody().string();
				compareContains("settlementTime", "SettlementTime should be in [000000,000001,000002,000003]", body);
			}
			case "000000", "000001", "000002", "000003" -> Assertions.assertThat(response.code()).isEqualTo(200);
		}
	}

	@Tag("eph")
	@ParameterizedTest
	@ValueSource(strings = { "size", "null", "empty" })
	void testPaymentRequestDTOWithJurisdictionalBody(String jurisdictionalBody) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidJurisdictionalBody(jurisdictionalBody);

		switch (jurisdictionalBody) {
			case "size" -> {
				Assertions.assertThat(response.code()).isEqualTo(400);
				String body = response.errorBody().string();
				compareContains("jurisdictionalBody", "Jurisdictional Body should be less than 255 characters", body);
			}
			case "null", "empty" -> Assertions.assertThat(response.code()).isEqualTo(200);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "invalid", "B", "C", "null", "empty" })
	void testPaymentRequestDTOWithPayeeType(String payeeType) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidPayeeType(payeeType);
		if (payeeType.equals("invalid")) {
			Assertions.assertThat(response.code()).isEqualTo(400);
			String body = response.errorBody().string();
			compareContains("payeeType", "Payee Type should be in [B, C]", body);
		}
		else {
			Assertions.assertThat(response.code()).isEqualTo(200);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty", "length" })
	void testPaymentRequestDTOWithPayeeName(String payeeName) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidPayeeName(payeeName);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		switch (payeeName) {
			case "null", "empty" -> compareContains("payeeName", "Payee Name should not be empty", body);
			case "length" -> compareContains("payeeName", "Payee Name must be between 1 and 100 characters.", body);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)

	// Line 1 is optional as per latest shared kernel version 4.0.19: null/empty should be
	// 200
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty", "length" })
	void testPaymentRequestDTOWithPayeeAddLine1(String addLine1) throws IOException {
		var response = invalidAddLine1(addLine1);
		switch (addLine1) {
			case "length" -> {
				Assertions.assertThat(response.code()).isEqualTo(400);
				String body = response.errorBody().string();
				compareContains("addLine1", "Address Line1 should be less than or equal to 100 characters", body);
			}
			case "null", "empty" -> Assertions.assertThat(response.code()).isEqualTo(200);
		}
	}

	// Line 2 is optional as per latest shared kernel version 4.0.19: null/empty should be
	// 200
	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "length", "null", "empty" })
	void testPaymentRequestDTOWithPayeeAddLine2(String addLine2) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidAddLine2(addLine2);

		if (addLine2.equals("length")) {
			Assertions.assertThat(response.code()).isEqualTo(400);
			String body = response.errorBody().string();
			compareContains("addLine2", "Address Line2 should be less than or equal to 100 characters", body);
		}
		else if (addLine2.equals("null") || addLine2.equals("empty")) {
			Assertions.assertThat(response.code()).isEqualTo(200);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty", "size" })
	void testPaymentRequestDTOWithPayeeState(String state) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidState(state);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		switch (state) {
			case "empty", "null" -> compareContains("state", "State cannot be null or empty.", body);
			case "size" -> compareContains("state", "State should be between 1 and 10 characters.", body);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty", "length" })
	void testPaymentRequestDTOWithPayeeCity(String city) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidCity(city);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		switch (city) {
			case "empty", "null" -> compareContains("city", "City cannot be null or empty.", body);
			case "length" -> compareContains("city", "City should be between 1 and 50 characters.", body);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty", "size" })
	void testPaymentRequestDTOWithPayeeCountry(String country) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidCountry(country);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		switch (country) {
			case "empty", "null" -> compareContains("country", "Country cannot be null or empty.", body);
			case "size" -> compareContains("country", "Country should be between 1 and 10 characters.", body);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty", "length" })
	void testPaymentRequestDTOWithPayeePostalCode(String postalCode) throws IOException {
		var response = invalidPostalCode(postalCode);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		switch (postalCode) {
			case "null", "empty" -> compareContains("postalCode", "Postal Code cannot be null", body);
			case "length" -> compareContains("postalCode", "Postal Code should be between 1 and 15 characters.", body);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "moreThan9", "lessThan9", "null", "empty", "invalid" })
	void testPaymentRequestDTOWithPayeeTaxId(String taxId) throws IOException {
		retrofit2.Response<ResponseBody> response = invalidTaxId(taxId);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		switch (taxId) {
			case "moreThan9", "lessThan9" -> compareContains("taxId", "TaxId should be 9 digits", body);
			case "null" -> compareContains("taxId", "TaxId should not be blank", body);
			case "empty" -> compareContains("taxId", "TaxId should be 9 digits", body);
			case "invalid" -> compareContains("taxId", "TaxId should be numeric", body);
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty" })
	void testPaymentRequestDTOWithPayeeNpiId(String npiId) throws IOException {
		var response = invalidNpiId(npiId);
		Assertions.assertThat(response.code()).isEqualTo(200);
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty", "length" })
	void testPaymentRequestDTOWithReceiverId(String receiverId) throws IOException {
		var response = invalidReceiverId(receiverId);
		Assertions.assertThat(response.code()).isEqualTo(400);
		String body = response.errorBody().string();

		switch (receiverId) {
			case "null", "empty" -> {
				compareContainsAny("receiverId", body, "receiverId is required",
						"Receiver Id should be between 1 and 50 characters.");
			}
			case "length" -> {
				compareContains("receiverId", "Receiver Id should be between 1 and 50 characters.", body);
			}
		}
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "null", "empty" })
	void testPaymentRequestDTOWithShippingDetails(String scenario) throws IOException {
		retrofit2.Response<ResponseBody> response = shippingAddress(scenario);
		Assertions.assertThat(response.code()).isEqualTo(200);
	}

	@Tag("eph")
	@Tag(LevelNames.GOLD)
	@Tag(LevelNames.SILVER)
	@ParameterizedTest
	@ValueSource(strings = { "negative", "zeroWithURL", "zeroWithoutURL", "greaterThanZeroWithURL",
			"greaterThanZeroWithOutURL" })
	void testPaymentRequestDTOWithClaimCountAndURL(String scenario) throws IOException {
		retrofit2.Response<ResponseBody> response = claimDetails(scenario);

		switch (scenario) {
			case "negative" -> {
				Assertions.assertThat(response.code()).isEqualTo(400);
				String body = response.errorBody().string();
				compareContains("claimCount", "TotalCount should be a non-negative integer", body);
			}
			case "zeroWithURL" -> {
				Assertions.assertThat(response.code()).isEqualTo(400);
				String body = response.errorBody().string();
				compareContains("claimCount", "Url should be null/empty if claim count is 0", body);
			}
			case "zeroWithoutURL", "greaterThanZeroWithURL" -> Assertions.assertThat(response.code()).isEqualTo(200);
			case "greaterThanZeroWithOutURL" -> {
				Assertions.assertThat(response.code()).isEqualTo(400);
				String body = response.errorBody().string();
				compareContains("claimCount", "Url should not be empty or null if claim count is greater than 0", body);
			}
		}
	}

}
