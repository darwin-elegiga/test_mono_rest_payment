package com.uhg.optum.financials.payments.features.paymentDtoValidation.implementation;

import com.uhg.optum.financials.payments.APITrigger.APIEndPointsImpl;
import com.uhg.optum.financials.payments.APITrigger.APITriggers;
import com.uhg.optum.financials.payments.common.BaseIntegrationTest;
import com.uhg.optum.financials.shared.client.dto.payment.ClaimsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import okhttp3.ResponseBody;
import retrofit2.Response;

import java.io.IOException;
import java.math.BigDecimal;

public abstract class PaymentRequestDTOValidationImpl extends BaseIntegrationTest {

	private final APITriggers apiTriggers = APITriggers.getAPITriggersInstance();

	protected final APIEndPointsImpl apiEndPointsImpl = APIEndPointsImpl.getAPIEndPointsImplInstance();

	/** Normalize Claims baseline (upper env): totalCount=0, url=null, plb=false */
	private void normalizeClaimUrlRulesForUpperEnv(PaymentRequestDto dto) {
		if (dto.getClaims() == null) {
			dto.setClaims(new ClaimsDto());
		}
		dto.getClaims().setTotalCount(0);
		dto.getClaims().setUrl(null);
		dto.getClaims().setPlb(false);
	}

	// -------------------------
	// Payment / Org identifiers
	// -------------------------
	public Response<ResponseBody> invalidOrigin(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "empty" -> paymentRequest.setOrigin("");
			case "null" -> paymentRequest.setOrigin(null);
			case "length" -> paymentRequest.setOrigin("1".repeat(51));
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public Response<ResponseBody> invalidCorrelationId(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		paymentRequest.setCorrelationId(scenario);
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidOrgShortCode(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		paymentRequest.setOrgShortCode(scenario);
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidAccountShortCode(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		paymentRequest.setOrgShortCode(getOrgShortCode());
		paymentRequest.setAccountShortCode(scenario);
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidSubAccountShortCode(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "null" -> paymentRequest.setSubAccountShortCode(null);
			case "empty" -> {
				paymentRequest.setOrgShortCode(getOrgShortCode());
				paymentRequest.setAccountShortCode(getAccountShortCode());
				paymentRequest.setSubAccountShortCode("");
			}
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	// -------------
	// Payment fields
	// -------------
	public retrofit2.Response<ResponseBody> invalidPaymentId(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "empty" -> paymentRequest.getPaymentDetails().setId("");
			case "null" -> paymentRequest.getPaymentDetails().setId(null);
			case "length" -> paymentRequest.getPaymentDetails().setId("q".repeat(33));
			case "duplicate" -> {
				String paymentNumber = createRandomNumber(10);
				paymentRequest.getPaymentDetails().setId(paymentNumber);
				this.apiEndPointsImpl.loadPayment(paymentRequest);
			}
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidIssuerPaymentId(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "empty" -> paymentRequest.getPaymentDetails().setIssuerPaymentId("");
			case "null" -> paymentRequest.getPaymentDetails().setIssuerPaymentId(null);
			case "length" -> paymentRequest.getPaymentDetails().setIssuerPaymentId("1".repeat(51));
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidReferenceId(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "length" -> paymentRequest.getPaymentDetails().setReferenceId("x".repeat(51));
			case "null" -> paymentRequest.getPaymentDetails().setReferenceId(null);
			case "empty" -> paymentRequest.getPaymentDetails().setReferenceId("");
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidPaymentDate(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "null" -> paymentRequest.getPaymentDetails().setPaymentDate(null);
			case "empty" -> paymentRequest.getPaymentDetails().setPaymentDate("");
			case "invalid" -> paymentRequest.getPaymentDetails().setPaymentDate("2022/01/01");
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidAdjudicationCutoffDate(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "null" -> paymentRequest.getPaymentDetails().setAdjudicationCutoffDate(null);
			case "empty" -> paymentRequest.getPaymentDetails().setAdjudicationCutoffDate("");
			case "invalid" -> paymentRequest.getPaymentDetails().setAdjudicationCutoffDate("2022/01/01");
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidAdjudicationVersion(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "length" -> paymentRequest.getPaymentDetails().setAdjudicationVersion("x".repeat(51));
			case "null" -> paymentRequest.getPaymentDetails().setAdjudicationVersion(null);
			case "empty" -> paymentRequest.getPaymentDetails().setAdjudicationVersion("");
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidAmount(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "null" -> paymentRequest.getPaymentDetails().setAmount(null);
			case "100M" -> paymentRequest.getPaymentDetails().setAmount(BigDecimal.valueOf(100000000));
			case "decimal" -> paymentRequest.getPaymentDetails().setAmount(BigDecimal.valueOf(345.001));
			case "negative" -> paymentRequest.getPaymentDetails().setAmount(BigDecimal.valueOf(-100));
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> setAmount(double scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		paymentRequest.getPaymentDetails().setAmount(BigDecimal.valueOf(scenario));
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidRemitMethod(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "null" -> paymentRequest.getPaymentDetails().setRemitMethod(null);
			case "empty" -> paymentRequest.getPaymentDetails().setRemitMethod("");
			case "invalid" -> paymentRequest.getPaymentDetails().setRemitMethod("test");
			case "PDF" -> paymentRequest.getPaymentDetails().setRemitMethod("PDF");
			case "DATA" -> paymentRequest.getPaymentDetails().setRemitMethod("DATA");
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	// ----------------
	// Other parameters
	// ----------------
	public retrofit2.Response<ResponseBody> invalidJurisdictionalBody(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "size" -> paymentRequest.getPaymentDetails().setJurisdictionalBody("x".repeat(256));
			case "null" -> paymentRequest.getPaymentDetails().setJurisdictionalBody(null);
			case "empty" -> paymentRequest.getPaymentDetails().setJurisdictionalBody("");
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidNumberOfAttachedPdfs(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "size" -> paymentRequest.getPaymentDetails().setNumberOfAttachedPdfs(12345);
			case "zero" -> paymentRequest.getPaymentDetails().setNumberOfAttachedPdfs(0);
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidSettlementTime(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "null" -> paymentRequest.getPaymentDetails().setSettlementTime(null);
			case "empty" -> paymentRequest.getPaymentDetails().setSettlementTime("");
			case "invalid" -> paymentRequest.getPaymentDetails().setSettlementTime("000008");
			case "000000", "000001", "000002", "000003" ->
				paymentRequest.getPaymentDetails().setSettlementTime(scenario);
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidPayeeType(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		if ("invalid".equals(scenario)) {
			paymentRequest.getPayeeDetails().setType("A");
		}
		else if ("null".equals(scenario)) {
			paymentRequest.getPayeeDetails().setType(null);
		}
		else if ("empty".equals(scenario)) {
			paymentRequest.getPayeeDetails().setType("");
		}
		else {
			paymentRequest.getPayeeDetails().setType(scenario); // "B" or "C"
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidPayeeName(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "empty" -> paymentRequest.getPayeeDetails().setName("");
			case "null" -> paymentRequest.getPayeeDetails().setName(null);
			case "length" -> paymentRequest.getPayeeDetails().setName("x".repeat(101));
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidTaxId(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "empty" -> paymentRequest.getPayeeDetails().setTaxId("");
			case "null" -> paymentRequest.getPayeeDetails().setTaxId(null);
			case "moreThan9" -> paymentRequest.getPayeeDetails().setTaxId("1234567890"); // 10
																							// digits
			case "lessThan9" -> paymentRequest.getPayeeDetails().setTaxId("12345678"); // 8
																						// digits
			case "invalid" -> paymentRequest.getPayeeDetails().setTaxId("ER4567854");
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidNpiId(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "null" -> paymentRequest.getPayeeDetails().setNpi(null);
			case "empty" -> paymentRequest.getPayeeDetails().setNpi("");
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidReceiverId(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		paymentRequest.setOrgShortCode(getOrgShortCode());
		paymentRequest.setAccountShortCode(getAccountShortCode());
		paymentRequest.setSubAccountShortCode(getSubAccountShortCode());

		switch (scenario) {
			case "empty" -> paymentRequest.getPayeeDetails().setReceiverId("");
			case "null" -> paymentRequest.getPayeeDetails().setReceiverId(null);
			case "length" -> paymentRequest.getPayeeDetails().setReceiverId("x".repeat(51));
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidShippingName(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		if ("length".equals(scenario)) {
			paymentRequest.getShippingDetails().setName("x".repeat(51));
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	// ---------------
	// Address fields
	// ---------------
	public retrofit2.Response<ResponseBody> invalidAddLine1(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "empty" -> paymentRequest.getPayeeDetails().getAddress().setLine1("");
			case "null" -> paymentRequest.getPayeeDetails().getAddress().setLine1(null);
			case "length" -> paymentRequest.getPayeeDetails().getAddress().setLine1("x".repeat(101));
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidAddLine2(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "length" -> paymentRequest.getPayeeDetails().getAddress().setLine2("x".repeat(101));
			case "null" -> paymentRequest.getPayeeDetails().getAddress().setLine2(null);
			case "empty" -> paymentRequest.getPayeeDetails().getAddress().setLine2("");
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidCity(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "empty" -> paymentRequest.getPayeeDetails().getAddress().setCity("");
			case "null" -> paymentRequest.getPayeeDetails().getAddress().setCity(null);
			case "length" -> paymentRequest.getPayeeDetails().getAddress().setCity("x".repeat(51));
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidState(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "empty" -> paymentRequest.getPayeeDetails().getAddress().setState("");
			case "null" -> paymentRequest.getPayeeDetails().getAddress().setState(null);
			case "size" -> paymentRequest.getPayeeDetails().getAddress().setState("X".repeat(11)); // >
																									// 10
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidCountry(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "empty" -> paymentRequest.getPayeeDetails().getAddress().setCountry("");
			case "null" -> paymentRequest.getPayeeDetails().getAddress().setCountry(null);
			case "size" -> paymentRequest.getPayeeDetails().getAddress().setCountry("X".repeat(11));
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> shippingAddress(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		paymentRequest.setOrgShortCode(getOrgShortCode());
		paymentRequest.setAccountShortCode(getAccountShortCode());
		paymentRequest.setSubAccountShortCode(getSubAccountShortCode());

		switch (scenario) {
			case "empty" -> {
				paymentRequest.getShippingDetails().setName("");
				paymentRequest.getShippingDetails().getAddress().setLine1("");
				paymentRequest.getShippingDetails().getAddress().setCity("");
				paymentRequest.getShippingDetails().getAddress().setState("");
				paymentRequest.getShippingDetails().getAddress().setCountry("");
				paymentRequest.getShippingDetails().getAddress().setPostalCode("");
			}
			case "null" -> paymentRequest.setShippingDetails(null);
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	public retrofit2.Response<ResponseBody> invalidPostalCode(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		// each scenario sets intended value only
		switch (scenario) {
			case "null" -> paymentRequest.getPayeeDetails().getAddress().setPostalCode(null);
			case "empty" -> paymentRequest.getPayeeDetails().getAddress().setPostalCode("");
			case "length" -> paymentRequest.getPayeeDetails().getAddress().setPostalCode("x".repeat(16));
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

	// ----------
	// Claims URL
	// ----------
	public retrofit2.Response<ResponseBody> claimDetails(String scenario) throws IOException {
		PaymentRequestDto paymentRequest = getPaymentRequest(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());

		// Start from a valid baseline so unrelated tests don't get polluted with
		// irrelevant errors
		normalizeClaimUrlRulesForUpperEnv(paymentRequest);

		switch (scenario) {
			case "negative" -> {
				paymentRequest.getClaims().setTotalCount(-1);
				paymentRequest.getClaims().setUrl(null);
				paymentRequest.getClaims().setPlb(false);
			}
			case "zeroWithURL" -> {
				paymentRequest.getClaims().setTotalCount(0);
				paymentRequest.getClaims().setUrl("https://example.com/doc");
				paymentRequest.getClaims().setPlb(false);
			}
			case "zeroWithoutURL", "zeroWithOutURL" -> {
				paymentRequest.getClaims().setTotalCount(0);
				paymentRequest.getClaims().setUrl(null);
				paymentRequest.getClaims().setPlb(false);
			}
			case "greaterThanZeroWithURL" -> {
				paymentRequest.getClaims().setTotalCount(1);
				paymentRequest.getClaims().setUrl("https://example.com/doc");
				paymentRequest.getClaims().setPlb(false);
			}
			case "greaterThanZeroWithOutURL" -> {
				paymentRequest.getClaims().setTotalCount(1);
				paymentRequest.getClaims().setUrl("");
				paymentRequest.getClaims().setPlb(false);
			}
			default -> {
			}
		}
		return this.apiTriggers.loadPaymentInVirtualCard(paymentRequest);
	}

}
