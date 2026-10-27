package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.bankaccount.events.BankAccountCreated;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.BankAccountEventDto;
import com.uhg.optum.financials.payments.payer.service.BankAccountService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import jakarta.xml.bind.ValidationException;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
public class BankAccountControllerTests {

	@InjectMocks
	private BankAccountController bankAccountController;

	@Mock
	private BankAccountService eventBusService;

	@Mock
	private LogUtility logUtility;

	@Test
	void testSendMessage() throws ValidationException {
		BankAccountCreated bankAccountCreated = new BankAccountCreated();
		bankAccountCreated.setRoutingNumber("123456789");
		bankAccountCreated.setAccountNumberLastFourDigits("1234");
		BankAccountEventDto bankAccountEventDto = new BankAccountEventDto();
		bankAccountEventDto.setBankAccountCreated(bankAccountCreated);
		Assertions.assertThat(this.bankAccountController.sendMessage(bankAccountEventDto)).isNotNull();

	}

}
