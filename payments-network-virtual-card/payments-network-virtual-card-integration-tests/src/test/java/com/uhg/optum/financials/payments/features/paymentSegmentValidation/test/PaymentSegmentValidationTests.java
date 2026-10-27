package com.uhg.optum.financials.payments.features.paymentSegmentValidation.test;

import com.uhg.optum.financials.payments.common.TestContext;
import com.uhg.optum.financials.payments.features.paymentSegmentValidation.implementation.PaymentSegmentValidationImpl;
import com.uhg.optum.financials.payments.payer.dto.PaymentResponseDto;
import com.uhg.optum.financials.shared.core.constants.LevelNames;
import lombok.extern.slf4j.Slf4j;
import org.json.JSONException;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.parallel.Execution;
import org.junit.jupiter.api.parallel.ExecutionMode;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;

import java.io.IOException;

@Slf4j
@Execution(ExecutionMode.CONCURRENT)
public class PaymentSegmentValidationTests extends PaymentSegmentValidationImpl {

	private static TestContext testContext;

	@BeforeAll
	static void setUp() throws IOException, JSONException {
		testContext = baseSetUp();

		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "VALID");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "BPR_I");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "BPR_H_Notification");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "BPR_H_PaymentOnly");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "BPR_D");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "SETTLEMENT_000001");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "SETTLEMENT_000002");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "SETTLEMENT_000003");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "ADJ_VS_VALID");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "ADJ_VS_NULL");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "ADJ_VS_EMPTY");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "ADJ_CUT_OFF_DATE_VALID");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "ADJ_CUT_OFF_DATE_NULL");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "ADJ_CUT_OFF_DATE_EMPTY");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "ADDRESS_WITH_LINE2");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "ADDRESS_WITHOUT_LINE2");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "NPI_Available");
		createAndLoadPaymentRequest(getOrg(), getAccount(), getSubAccount(), "NPI_Not_available");

	}

	private static String getOrg() {
		return testContext.orgShortCode();
	}

	private static String getAccount() {
		return testContext.accountShortCode();
	}

	private static String getSubAccount() {
		return testContext.subAccountShortCode();
	}

	@Override
	protected String getOrgShortCode() {
		return getOrg();
	}

	@Override
	protected String getAccountShortCode() {
		return getAccount();
	}

	@Override
	protected String getSubAccountShortCode() {
		return getSubAccount();
	}

	@Test
	// @Tag("eph")
	@Tag(LevelNames.SILVER)
	@Tag(LevelNames.GOLD)
	void validatingBPRSegmentFor_I_Case() throws IOException, InterruptedException {
		String paymentId = scenarioPaymentNumMap.get("BPR_I");
		apiEndPointsImpl.isPaymentLoaded(paymentId);

		apiEndPointsImpl.waitTillJobCompleted(paymentId, "PR");

		PaymentResponseDto paymentReponseDto = apiEndPointsImpl.getPaymentDetails(paymentId);
		String[] BPRValues = getSegmentValues(paymentReponseDto.getStSeSegment(), "\nBPR");
		// validateBPR_Segment_I_Case(paymentReponseDto, BPRValues);
		validateBPRSegment(paymentReponseDto, BPRValues, "I");

	}

	@Test
	@Tag(LevelNames.SILVER)
	@Tag(LevelNames.GOLD)
	// @Tag("eph")
	void validatingBPRSegmentFor_H_Case() throws IOException {
		String paymentId = scenarioPaymentNumMap.get("BPR_H_Notification");
		apiEndPointsImpl.isPaymentLoaded(paymentId);

		apiEndPointsImpl.waitTillJobCompleted(paymentId, "PR");
		PaymentResponseDto paymentReponseDto = apiEndPointsImpl.getPaymentDetails(paymentId);
		String[] BPRValues = getSegmentValues(paymentReponseDto.getStSeSegment(), "\nBPR");
		validateBPR_Segment_H_Case(paymentReponseDto, BPRValues);

	}

	@Test
	@Tag(LevelNames.SILVER)
	@Tag(LevelNames.GOLD)
	// @Tag("eph")
	void validatingBPRSegmentFor_H_PaymentOnly_Case() throws IOException {
		String paymentId = scenarioPaymentNumMap.get("BPR_H_PaymentOnly");
		apiEndPointsImpl.isPaymentLoaded(paymentId);

		apiEndPointsImpl.waitTillJobCompleted(paymentId, "PR");
		PaymentResponseDto paymentReponseDto = apiEndPointsImpl.getPaymentDetails(paymentId);
		String[] BPRValues = getSegmentValues(paymentReponseDto.getStSeSegment(), "\nBPR");
		validateBPR_Segment_H_PaymentOnly_Case(paymentReponseDto, BPRValues);

	}

	@Test
	@Tag(LevelNames.SILVER)
	@Tag(LevelNames.GOLD)
	// @Tag("eph")
	void validatingBPRSegment_BPR_D() throws IOException {
		String paymentId = scenarioPaymentNumMap.get("BPR_D");
		apiEndPointsImpl.isPaymentLoaded(paymentId);

		apiEndPointsImpl.waitTillJobCompleted(paymentId, "PR");
		PaymentResponseDto paymentReponseDto = apiEndPointsImpl.getPaymentDetails(paymentId);
		String[] BPRValues = getSegmentValues(paymentReponseDto.getStSeSegment(), "\nBPR");
		validateBPRSegment(paymentReponseDto, BPRValues, "D");

	}

	@ParameterizedTest
	@Tag(LevelNames.SILVER)
	@Tag(LevelNames.GOLD)
	// @Tag("eph")
	@ValueSource(strings = { "000001", "000002", "000003" })
	void validating_BPR_SegmentForSettlementTime(String settlementTime) throws IOException {

		String paymentId = scenarioPaymentNumMap.get("SETTLEMENT_" + settlementTime);
		apiEndPointsImpl.isPaymentLoaded(paymentId);

		apiEndPointsImpl.waitTillJobCompleted(paymentId, "PR");

		PaymentResponseDto paymentReponseDto = apiEndPointsImpl.getPaymentDetails(paymentId);
		String[] BPRValues = getSegmentValues(paymentReponseDto.getStSeSegment(), "\nBPR");

		compareEquals("BPR fields count", 17, BPRValues.length);
		compareEquals("BPR16", getFormatedDate(calculateSettlementDate(settlementTime)), BPRValues[16]);
	}

	@Test
	// @Tag("eph")
	@Tag(LevelNames.SILVER)
	@Tag(LevelNames.GOLD)
	void validating_TRN_Segment() throws IOException {
		String paymentId = scenarioPaymentNumMap.get("VALID");
		apiEndPointsImpl.isPaymentLoaded(paymentId);

		apiEndPointsImpl.waitTillJobCompleted(paymentId, "PR");

		PaymentResponseDto paymentReponseDto = apiEndPointsImpl.getPaymentDetails(paymentId);
		String[] TRNValues = getSegmentValues(paymentReponseDto.getStSeSegment(), "\nTRN");
		validateTRN_Segment(paymentReponseDto, TRNValues);
	}

	@Test
	@Tag(LevelNames.SILVER)
	@Tag(LevelNames.GOLD)
	// @Tag("eph")
	void validating_REF_EV_Segment() throws IOException {
		String paymentId = scenarioPaymentNumMap.get("VALID");
		apiEndPointsImpl.isPaymentLoaded(paymentId);

		apiEndPointsImpl.waitTillJobCompleted(paymentId, "PR");

		PaymentResponseDto paymentReponseDto = apiEndPointsImpl.getPaymentDetails(paymentId);
		String[] REF_EV_Values = getSegmentValues(paymentReponseDto.getStSeSegment(), "\nREF", "EV");
		validateREFEV_Segment(paymentReponseDto, REF_EV_Values);
	}

	@ParameterizedTest
	@Tag(LevelNames.SILVER)
	@Tag(LevelNames.GOLD)
	// @Tag("eph")
	@ValueSource(strings = { "ADJ_VS_VALID", "ADJ_VS_NULL", "ADJ_VS_EMPTY" })
	void validating_REF_F2_Segment(String scenario) throws IOException {
		String paymentId = scenarioPaymentNumMap.get(scenario);
		apiEndPointsImpl.isPaymentLoaded(paymentId);

		apiEndPointsImpl.waitTillJobCompleted(paymentId, "PR");

		PaymentResponseDto paymentReponseDto = apiEndPointsImpl.getPaymentDetails(paymentId);
		String[] REF_F2_Values = getSegmentValues(paymentReponseDto.getStSeSegment(), "\nREF", "F2");
		validateREFF2_Segment(paymentReponseDto, scenario, REF_F2_Values);
	}

	@ParameterizedTest
	@Tag(LevelNames.SILVER)
	@Tag(LevelNames.GOLD)
	// @Tag("eph")
	@ValueSource(strings = { "ADJ_CUT_OFF_DATE_VALID", "ADJ_CUT_OFF_DATE_NULL", "ADJ_CUT_OFF_DATE_EMPTY" })
	void validating_DTM_Segment(String scenario) throws IOException {
		String paymentId = scenarioPaymentNumMap.get(scenario);
		apiEndPointsImpl.isPaymentLoaded(paymentId);

		apiEndPointsImpl.waitTillJobCompleted(paymentId, "PR");

		PaymentResponseDto paymentReponseDto = apiEndPointsImpl.getPaymentDetails(paymentId);
		String[] DTM_Values = getSegmentValues(paymentReponseDto.getStSeSegment(), "\nDTM", "405");
		validate_DTM_Segment(paymentReponseDto, scenario, DTM_Values);
	}

	@Test
	// @Tag("eph")
	@Tag(LevelNames.SILVER)
	@Tag(LevelNames.GOLD)
	void validating_REF_2U_Segment() throws IOException {
		String paymentId = scenarioPaymentNumMap.get("VALID");
		apiEndPointsImpl.isPaymentLoaded(paymentId);

		apiEndPointsImpl.waitTillJobCompleted(paymentId, "PR");

		PaymentResponseDto paymentReponseDto = apiEndPointsImpl.getPaymentDetails(paymentId);
		String[] REF_2U_Values = getSegmentValues(paymentReponseDto.getStSeSegment(), "\nREF", "2U");
		validate_REF_2U_Segment(REF_2U_Values);
	}

	@Test
	// @Tag("eph")
	@Tag(LevelNames.SILVER)
	@Tag(LevelNames.GOLD)
	void validating_REF_EO_Segment() throws IOException {
		String paymentId = scenarioPaymentNumMap.get("VALID");
		apiEndPointsImpl.isPaymentLoaded(paymentId);

		apiEndPointsImpl.waitTillJobCompleted(paymentId, "PR");

		PaymentResponseDto paymentReponseDto = apiEndPointsImpl.getPaymentDetails(paymentId);
		String[] REF_EO_Values = getSegmentValues(paymentReponseDto.getStSeSegment(), "\nREF", "EO");
		validate_REF_EO_Segment(REF_EO_Values);
	}

	@ParameterizedTest
	@Tag(LevelNames.SILVER)
	@Tag(LevelNames.GOLD)
	// @Tag("eph")
	@ValueSource(strings = { "ADDRESS_WITH_LINE2", "ADDRESS_WITHOUT_LINE2" })
	void validating_N1_PE_Segment(String scenario) throws IOException {
		String paymentId = scenarioPaymentNumMap.get(scenario);
		apiEndPointsImpl.isPaymentLoaded(paymentId);

		apiEndPointsImpl.waitTillJobCompleted(paymentId, "PR");

		PaymentResponseDto paymentReponseDto = apiEndPointsImpl.getPaymentDetails(paymentId);
		String payeeDetails = getN1Details("PE", paymentReponseDto.getStSeSegment());
		validate_N1_PE_Segment(paymentReponseDto, scenario, payeeDetails);
	}

	@ParameterizedTest
	@Tag(LevelNames.SILVER)
	@Tag(LevelNames.GOLD)
	// @Tag("eph")
	@ValueSource(strings = { "NPI_Available", "NPI_Not_available" })
	void validating_N1_PE_REF_TJ_Segment(String scenario) throws IOException {
		String paymentId = scenarioPaymentNumMap.get(scenario);
		apiEndPointsImpl.isPaymentLoaded(paymentId);

		apiEndPointsImpl.waitTillJobCompleted(paymentId, "PR");

		PaymentResponseDto paymentReponseDto = apiEndPointsImpl.getPaymentDetails(paymentId);
		String[] N1_PE_Segment = getSegmentValues(paymentReponseDto.getStSeSegment(), "\nN1", "PE");
		validate_N1_PE_Segment(paymentReponseDto, scenario, N1_PE_Segment);

		String[] REF_TJ_Segment = getSegmentValues(paymentReponseDto.getStSeSegment(), "\nREF", "TJ");
		validate_REF_TJ_Segment(paymentReponseDto, scenario, REF_TJ_Segment);
	}

}
