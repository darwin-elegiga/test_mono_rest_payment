package com.uhg.optum.financials.payments.features.paymentSegmentValidation.implementation;

import com.uhg.optum.financials.payments.APITrigger.APIEndPointsImpl;
import com.uhg.optum.financials.payments.common.BaseIntegrationTest;
import com.uhg.optum.financials.payments.payer.dto.PaymentResponseDto;
import com.uhg.optum.financials.payments.payer.dto.account.AccountDto;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import org.assertj.core.api.Assertions;

import java.io.IOException;

public abstract class PaymentSegmentValidationImpl extends BaseIntegrationTest {

	protected APIEndPointsImpl apiEndPointsImpl = APIEndPointsImpl.getAPIEndPointsImplInstance();

	public void validateBPRSegment(PaymentResponseDto paymentRequest, String[] bprValues, String bpr01)
			throws IOException {
		compareEquals("BPR fields count", 17, bprValues.length);

		AccountDto payerDTO = this.apiEndPointsImpl.getAccountDetails(getOrgShortCode(), getAccountShortCode());
		compareEquals("BPR header", "\nBPR", bprValues[0]);
		compareEquals("BPR01", bpr01, bprValues[1]);
		compareEquals("BPR02", paymentRequest.getPaymentDetails().getAmount().doubleValue(),
				Double.parseDouble(bprValues[2]));
		compareEquals("BPR03", "C", bprValues[3]);
		compareEquals("BPR04", "ACH", bprValues[4]);
		compareEquals("BPR05", "CCP", bprValues[5]);
		compareEquals("BPR06", "01", bprValues[6]);
		compareEquals("BPR07", 9, bprValues[7].length());
		compareEquals("BPR08", "DA", bprValues[8]);
		Assertions.assertThat(bprValues[9]).isNotEmpty();
		compareEquals("BPR10", getSubAccountShortCode(), bprValues[10]);
		compareEquals("BPR11", "0000" + payerDTO.getAccountRegistered().getPayer835Id(), bprValues[11]);
		compareEquals("BPR12", "01", bprValues[12]);
		compareEquals("BPR13", "999999999", bprValues[13]);
		compareEquals("BPR14", "DA", bprValues[14]);
		compareEquals("BPR15", "99999999999999999", bprValues[15]);
		compareEquals("BPR16", getFormatedDate(calculateSettlementDate("000000")), bprValues[16]);
	}

	public void validateBPR_Segment_H_Case(PaymentRequestDto PaymentRequest, String[] BPRValues) throws IOException {
		compareEquals("BPR fields count", 17, BPRValues.length);
		compareEquals("BPR header", "\nBPR", BPRValues[0]);
		compareEquals("BPR01", "H", BPRValues[1]);
		compareEquals("BPR02", "0", BPRValues[2]);
		compareEquals("BPR03", "C", BPRValues[3]);
		compareEquals("BPR04", "NON", BPRValues[4]);
		compareEquals("BPR05", "", BPRValues[5]);
		compareEquals("BPR06", "", BPRValues[6]);
		compareEquals("BPR07", "", BPRValues[7]);
		compareEquals("BPR08", "", BPRValues[8]);
		compareEquals("BPR09", "", BPRValues[9]);
		compareEquals("BPR10", "", BPRValues[10]);
		compareEquals("BPR11", "", BPRValues[11]);
		compareEquals("BPR12", "", BPRValues[12]);
		compareEquals("BPR13", "", BPRValues[13]);
		compareEquals("BPR14", "", BPRValues[14]);
		compareEquals("BPR15", "", BPRValues[15]);
		compareEquals("BPR16", getFormatedDate(calculateSettlementDate("000000")), BPRValues[16]);
	}

	public void validateBPR_Segment_H_PaymentOnly_Case(PaymentRequestDto paymentRequest, String[] bprValues)
			throws IOException {
		compareEquals("BPR fields count", 17, bprValues.length);
		compareEquals("BPR header", "\nBPR", bprValues[0]);
		compareEquals("BPR01", "H", bprValues[1]);
		compareEquals("BPR02", "0", bprValues[2]);
		compareEquals("BPR03", "D", bprValues[3]);
		compareEquals("BPR04", "NON", bprValues[4]);
		compareEquals("BPR05", "", bprValues[5]);
		compareEquals("BPR06", "", bprValues[6]);
		compareEquals("BPR07", "", bprValues[7]);
		compareEquals("BPR08", "", bprValues[8]);
		compareEquals("BPR09", "", bprValues[9]);
		compareEquals("BPR10", "", bprValues[10]);
		compareEquals("BPR11", "", bprValues[11]);
		compareEquals("BPR12", "", bprValues[12]);
		compareEquals("BPR13", "", bprValues[13]);
		compareEquals("BPR14", "", bprValues[14]);
		compareEquals("BPR15", "", bprValues[15]);
		compareEquals("BPR16", getFormatedDate(calculateSettlementDate("000000")), bprValues[16]);
	}

	public void validateTRN_Segment(PaymentRequestDto paymentRequest, String[] TRNValues) throws IOException {
		AccountDto payerDTO = this.apiEndPointsImpl.getAccountDetails(getOrgShortCode(), getAccountShortCode());
		SubAccountDto subPayerDTO = this.apiEndPointsImpl.getSubAccountDetails(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		compareEquals("TRN fields count", 5, TRNValues.length);
		compareEquals("TRN header", "\nTRN", TRNValues[0]);
		compareEquals("TRN01", "1", TRNValues[1]);
		// TRN02 is expected to carry the generated network payment identifier.
		Assertions.assertThat(TRNValues[2]).isNotBlank();
		String paymentType = String.valueOf(paymentRequest.getPaymentDetails().getType());
		String expectedNetworkPrefix = "C2B".equalsIgnoreCase(paymentType) ? "CB" : "BB";
		Assertions.assertThat(TRNValues[2]).startsWith(expectedNetworkPrefix);
		Assertions.assertThat(TRNValues[2]).matches("^" + expectedNetworkPrefix + "[A-Z0-9_]+$");
		compareEquals("TRN03", getSubAccountShortCode(), TRNValues[3]);
		compareEquals("TRN04", "0000" + payerDTO.getAccountRegistered().getPayer835Id(), TRNValues[4]);
	}

	public void validateREFEV_Segment(PaymentRequestDto paymentRequest, String[] REFEVValues) {

		compareEquals("REFEV fields count", 3, REFEVValues.length);
		compareEquals("REFEV header", "\nREF", REFEVValues[0]);
		compareEquals("REFEV01", "EV", REFEVValues[1]);
		compareEquals("REFEV02", paymentRequest.getPayeeDetails().getReceiverId(), REFEVValues[2]);

	}

	public void validateREFF2_Segment(PaymentRequestDto paymentRequest, String scenario, String[] REFF2Values)
			throws IOException {
		if (scenario.equals("ADJ_VS_NULL") || scenario.equals("ADJ_VS_EMPTY")) {
			compareEquals("REF*F2 fields count", null, REFF2Values);
		}
		else {
			compareEquals("REF*F2 fields count", 3, REFF2Values.length);
			compareEquals("REF*F2 header", "\nREF", REFF2Values[0]);
			compareEquals("REF*F2 01", "F2", REFF2Values[1]);
			compareEquals("REF*F2 02", paymentRequest.getPaymentDetails().getAdjudicationVersion(), REFF2Values[2]);
		}
	}

	public void validate_DTM_Segment(PaymentRequestDto paymentRequest, String scenario, String[] DTMValues) {
		if (scenario.equals("ADJ_CUT_OFF_DATE_NULL") || scenario.equals("ADJ_CUT_OFF_DATE_EMPTY")) {
			compareEquals("DTM fields count", null, DTMValues);
		}
		else {
			compareEquals("DTM fields count", 3, DTMValues.length);
			compareEquals("DTM header", "\nDTM", DTMValues[0]);
			compareEquals("DTM 01", "405", DTMValues[1]);
			compareEquals("DTM 02", getFormatedDate(paymentRequest.getPaymentDetails().getAdjudicationCutoffDate()),
					DTMValues[2]);
		}
	}

	public void validate_REF_2U_Segment(String[] segmentValues) throws IOException {
		AccountDto payerDTO = this.apiEndPointsImpl.getAccountDetails(getOrgShortCode(), getAccountShortCode());
		compareEquals("REF*2U fields count", 3, segmentValues.length);
		compareEquals("REF*2U header", "\nREF", segmentValues[0]);
		compareEquals("REF*2U 01", "2U", segmentValues[1]);
		compareEquals("REF*2U 02", payerDTO.getAccountRegistered().getPayer835Id(), segmentValues[2]);

	}

	public void validate_REF_EO_Segment(String[] segmentValues) throws IOException {
		SubAccountDto subPayerDTO = this.apiEndPointsImpl.getSubAccountDetails(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		compareEquals("REF*EO fields count", 3, segmentValues.length);
		compareEquals("REF*EO header", "\nREF", segmentValues[0]);
		compareEquals("REF*EO 01", "EO", segmentValues[1]);
		compareEquals("REF*EO 02", getSubAccountShortCode(), segmentValues[2]);

	}

	public void validate_N1_PE_Segment(PaymentRequestDto paymentRequest, String scenario, String payeeDetails) {
		String[] segments = payeeDetails.split("\n");
		String[] N1_PE = segments[1].split("\\*");
		String[] N3 = segments[2].split("\\*");
		String[] N4 = segments[3].split("\\*");

		compareEquals("N1_PE count", 5, N1_PE.length);
		compareEquals("N1_PE header", "N1", N1_PE[0]);
		compareEquals("N1_PE 01", "PE", N1_PE[1]);
		compareEquals("N1_PE 02", paymentRequest.getPayeeDetails().getName(), N1_PE[2]);

		if (scenario.equals("ADDRESS_WITH_LINE2")) {

			compareEquals("N3 count", 3, N3.length);
			compareEquals("N3 02", paymentRequest.getPayeeDetails().getAddress().getLine2(), N3[2]);

		}
		else if (scenario.equals("ADDRESS_WITHOUT_LINE2")) {
			compareEquals("N3 count", 2, N3.length);

		}

		compareEquals("N3 header", "N3", N3[0]);
		compareEquals("N3 01", paymentRequest.getPayeeDetails().getAddress().getLine1(), N3[1]);

		compareEquals("N4 count", 4, N4.length);
		compareEquals("N4 header", "N4", N4[0]);
		compareEquals("N4 01", paymentRequest.getPayeeDetails().getAddress().getCity(), N4[1]);
		compareEquals("N4 02", paymentRequest.getPayeeDetails().getAddress().getState(), N4[2]);
		compareEquals("N4 03", paymentRequest.getPayeeDetails().getAddress().getPostalCode(), N4[3]);

	}

	public void validate_N1_PE_Segment(PaymentRequestDto paymentRequest, String scenario, String[] N1_PESegment) {

		compareEquals("N1_PE count", 5, N1_PESegment.length);
		compareEquals("N1_PE header", "\nN1", N1_PESegment[0]);
		compareEquals("N1_PE 01", "PE", N1_PESegment[1]);
		compareEquals("N1_PE 02", paymentRequest.getPayeeDetails().getName(), N1_PESegment[2]);
		if (scenario.equals("NPI_Available")) {
			compareEquals("N1_PE 03", "XX", N1_PESegment[3]);
			compareEquals("N1_PE 04", paymentRequest.getPayeeDetails().getNpi(), N1_PESegment[4]);
		}
		else {
			compareEquals("N1_PE 03", "FI", N1_PESegment[3]);
			compareEquals("N1_PE 04", paymentRequest.getPayeeDetails().getTaxId(), N1_PESegment[4]);
		}

	}

	public void validate_REF_TJ_Segment(PaymentRequestDto paymentRequest, String scenario, String[] REF_TJ_Segment) {
		if (scenario.equals("NPI_Available")) {
			compareEquals("REF*TJ count", 3, REF_TJ_Segment.length);
			compareEquals("REF*TJ header", "\nREF", REF_TJ_Segment[0]);
			compareEquals("REF*TJ 01", "TJ", REF_TJ_Segment[1]);
			compareEquals("REF*TJ 02", paymentRequest.getPayeeDetails().getTaxId(), REF_TJ_Segment[2]);
		}
		else {

			compareEquals("REF*TJ count", null, REF_TJ_Segment);

		}

	}

}
