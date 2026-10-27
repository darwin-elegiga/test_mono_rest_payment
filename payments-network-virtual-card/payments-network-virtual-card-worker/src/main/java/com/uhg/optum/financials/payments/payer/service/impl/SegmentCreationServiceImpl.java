package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountBankAccountDetails;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.service.EncryptionAtRestService;
import com.uhg.optum.financials.payments.payer.service.SegmentCreationService;
import com.uhg.optum.financials.payments.payer.util.SettlementDateUtil;
import com.uhg.optum.financials.shared.client.dto.payment.AddressDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.client.dto.payment.SubAccountDetailsDto;
import com.uhg.optum.financials.shared.lib835.service.SegmentBuilderService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.time.LocalDate;

@Slf4j
@Service
@RequiredArgsConstructor
public class SegmentCreationServiceImpl implements SegmentCreationService {

	private final SegmentBuilderService segmentBuilder;

	private final EncryptionAtRestService encryptionAtRestService;

	@Value("${holidays.list}")
	private String holidaysList;

	@Override
	public String createSegment(PaymentRequestDto requestDto, SubAccountBankAccountDetails subPayerBankAccount,
			LocalDate date, String networkPaymentId) {

		if (subPayerBankAccount == null || subPayerBankAccount.getBankAccountNumber() == null) {
			throw new NotFoundException("Bank account details not found to process payment");
		}

		LocalDate settlementDate = SettlementDateUtil.calculateSettlementDate(this.holidaysList,
				requestDto.getPaymentDetails().getSettlementTime(), (date != null) ? date : LocalDate.now());

		subPayerBankAccount
			.setBankAccountNumber(this.encryptionAtRestService.decrypt(subPayerBankAccount.getBankAccountNumber()));
		SubAccountDetailsDto payerDetailsDto = this.getPayerDetailsDto(subPayerBankAccount, settlementDate);

		// Delegate to kernel service to build final segment string
		return this.segmentBuilder.buildSTSESegment(requestDto, payerDetailsDto, networkPaymentId);
	}

	private SubAccountDetailsDto getPayerDetailsDto(SubAccountBankAccountDetails subPayerBankAccount,
			LocalDate settlementDate) {
		SubAccount subAccount = subPayerBankAccount.getSubAccount();
		SubAccountDetailsDto subAccountDetailsDto = new SubAccountDetailsDto();
		subAccountDetailsDto.setTin(subAccount.getTin());
		subAccountDetailsDto.setPayer835Id(subPayerBankAccount.getPayer835Id());
		subAccountDetailsDto.setName(subAccount.getName());
		subAccountDetailsDto.setSettlementDate(settlementDate);
		subAccountDetailsDto.setContactName(subAccount.getContactName());
		subAccountDetailsDto.setRoutingNumber(subPayerBankAccount.getBankRoutingNumber());
		subAccountDetailsDto.setBankAccountNumber(subPayerBankAccount.getBankAccountNumber());
		subAccountDetailsDto.setFaxNumber(subAccount.getFaxNumber());
		subAccountDetailsDto.setContactEmail(subAccount.getContactEmail());
		subAccountDetailsDto.setContactNumber(subAccount.getPhoneNumber());
		subAccountDetailsDto.setTechnicalContactName(subAccount.getTechnicalContactName());
		subAccountDetailsDto.setTechnicalPhoneNumber(subAccount.getTechnicalPhoneNumber());
		subAccountDetailsDto.setTechnicalPhoneNumberExtension(subAccount.getTechnicalPhoneNumberExtension());
		subAccountDetailsDto.setTechnicalEmailAddress(subAccount.getTechnicalEmailAddress());
		subAccountDetailsDto.setTechnicalURL(subAccount.getTechnicalURL());
		subAccountDetailsDto.setClaimOfficePhoneNumber(subAccount.getClaimOfficePhoneNumber());
		subAccountDetailsDto.setClaimOfficePhoneNumberExtension(subAccount.getClaimOfficePhoneNumberExtension());
		AddressDto address = new AddressDto(subAccount.getAddress().getLine1(), subAccount.getAddress().getLine2(),
				subAccount.getAddress().getCity(), subAccount.getAddress().getState(),
				subAccount.getAddress().getCountry(), subAccount.getAddress().getPostalCode());
		subAccountDetailsDto.setAddress(address);
		return subAccountDetailsDto;
	}

}
