package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.repository.OrganizationRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import org.springframework.stereotype.Component;

import java.util.List;

import static com.uhg.optum.financials.payments.payer.enums.Status.PE;

@Component
public class PaymentRequestUtil {

	PaymentRequestRepository paymentRequestRepository;

	OrganizationRepository organizationRegisteredRepository;

	public PaymentRequestUtil(PaymentRequestRepository paymentRequestRepository,
			OrganizationRepository organizationRegisteredRepository) {
		this.paymentRequestRepository = paymentRequestRepository;
		this.organizationRegisteredRepository = organizationRegisteredRepository;
	}

	public void checkDuplicatePayment(PaymentRequestDto paymentRequestDto) {
		if (this.paymentRequestRepository.existsByPaymentDetailsId(paymentRequestDto.getPaymentDetails().getId())) {
			throw new BadRequestException("Payment Already Exists");
		}
	}

	public void validateMandatoryFields(PaymentRequestDto paymentRequestDto) {
		if (paymentRequestDto.getPayeeDetails().getReceiverId() == null
				|| paymentRequestDto.getPayeeDetails().getReceiverId().isEmpty()) {
			throw new BadRequestException("receiverId is required");
		}
		if (paymentRequestDto.getClaims() == null || paymentRequestDto.getClaims().getTotalCount() == null) {
			throw new BadRequestException("claims.totalCount is required");
		}
	}

	public void checkOrgAccountSubAccountExistence(PaymentRequestDto paymentRequest) {
		List<Boolean> result = this.organizationRegisteredRepository
			.checkOrgAccountSubAccountExistence(paymentRequest.getOrgShortCode(), paymentRequest.getAccountShortCode(),
					paymentRequest.getSubAccountShortCode())
			.getFirst();
		boolean orgExists = result.get(0);
		boolean accountExists = result.get(1);
		boolean subAccountExists = result.get(2);

		if (!orgExists) {
			throw new BadRequestException("org not found");
		}
		if (!accountExists) {
			throw new BadRequestException("account not found");
		}
		if (!subAccountExists) {
			throw new BadRequestException("subAccount not found");
		}
	}

	public void checkPaymentForReprocessing(PaymentRequestDto paymentRequestDto) {
		PaymentRequest paymentRequest = this.paymentRequestRepository
			.findByPaymentId(paymentRequestDto.getPaymentDetails().getId());
		if (paymentRequest == null) {
			throw new BadRequestException("Payment Not Found for Reprocessing");
		}
		if (!PE.name().equals(paymentRequest.getStatus())) {
			throw new BadRequestException("Payment is not in a reprocessable state");
		}
	}

}
