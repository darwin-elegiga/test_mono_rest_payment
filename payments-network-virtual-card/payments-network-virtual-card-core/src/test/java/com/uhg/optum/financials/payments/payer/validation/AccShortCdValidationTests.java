package com.uhg.optum.financials.payments.payer.validation;

import com.uhg.optum.financials.payments.payer.dto.SpendRulesAccountDto;
import com.uhg.optum.financials.payments.payer.dto.SpendRulesDTO;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.BDDMockito;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import javax.validation.ConstraintValidatorContext;
import javax.validation.ConstraintValidatorContext.ConstraintViolationBuilder;
import javax.validation.ConstraintValidatorContext.ConstraintViolationBuilder.NodeBuilderCustomizableContext;
import java.util.ArrayList;
import java.util.List;
import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.verify;
import static org.mockito.Mockito.mock;

class AccShortCdValidationTests {

	AccShortCdValidation accShortCdValidation;

	@Mock
	ConstraintValidatorContext context;

	@Mock
	ConstraintViolationBuilder violationBuilder;

	NodeBuilderCustomizableContext nodeBuilderCustomizableContext;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
		this.accShortCdValidation = new AccShortCdValidation();
		this.nodeBuilderCustomizableContext = mock(NodeBuilderCustomizableContext.class);
	}

	@Test
	void initialize_shouldPrintValidator() {
		AccShortCdValidator validator = mock(AccShortCdValidator.class);
		this.accShortCdValidation.initialize(validator);
		// No assertion needed, just for coverage
	}

	@Test
	void isValid_shouldReturnTrue_whenAccountsIsNull() {
		SpendRulesDTO dto = mock(SpendRulesDTO.class);
		given(dto.getAccounts()).willReturn(null);
		boolean result = this.accShortCdValidation.isValid(dto, this.context);
		assertThat(result).isTrue();
	}

	@Test
	void isValid_shouldReturnFalse_whenFirstAccountIsNull() {
		List<SpendRulesAccountDto> accounts = new ArrayList<>();
		accounts.add(null);
		SpendRulesDTO dto = mock(SpendRulesDTO.class);
		given(dto.getAccounts()).willReturn(accounts);
		given(this.context.buildConstraintViolationWithTemplate(BDDMockito.anyString()))
			.willReturn(this.violationBuilder);
		given(this.violationBuilder.addPropertyNode(BDDMockito.anyString()))
			.willReturn(this.nodeBuilderCustomizableContext);
		boolean result = this.accShortCdValidation.isValid(dto, this.context);
		assertThat(result).isFalse();
		verify(this.context).disableDefaultConstraintViolation();
		verify(this.context)
			.buildConstraintViolationWithTemplate("Account Short Code is required when accounts are provided");
		verify(this.violationBuilder).addPropertyNode("accounts");
		verify(this.nodeBuilderCustomizableContext).addConstraintViolation();
	}

	@Test
	void isValid_shouldReturnFalse_whenAccShortCdIsEmpty() {
		SpendRulesAccountDto account = mock(SpendRulesAccountDto.class);
		given(account.getAccShortCd()).willReturn("");
		List<SpendRulesAccountDto> accounts = new ArrayList<>();
		accounts.add(account);
		SpendRulesDTO dto = mock(SpendRulesDTO.class);
		given(dto.getAccounts()).willReturn(accounts);
		given(this.context.buildConstraintViolationWithTemplate(BDDMockito.anyString()))
			.willReturn(this.violationBuilder);
		given(this.violationBuilder.addPropertyNode(BDDMockito.anyString()))
			.willReturn(this.nodeBuilderCustomizableContext);
		boolean result = this.accShortCdValidation.isValid(dto, this.context);
		assertThat(result).isFalse();
		verify(this.context).disableDefaultConstraintViolation();
		verify(this.context)
			.buildConstraintViolationWithTemplate("Account Short Code is required when accounts are provided");
		verify(this.violationBuilder).addPropertyNode("accounts");
		verify(this.nodeBuilderCustomizableContext).addConstraintViolation();
	}

	@Test
	void isValid_shouldReturnTrue_whenAccShortCdIsNotEmpty() {
		SpendRulesAccountDto account = mock(SpendRulesAccountDto.class);
		given(account.getAccShortCd()).willReturn("SOME_CODE");
		List<SpendRulesAccountDto> accounts = new ArrayList<>();
		accounts.add(account);
		SpendRulesDTO dto = mock(SpendRulesDTO.class);
		given(dto.getAccounts()).willReturn(accounts);
		boolean result = this.accShortCdValidation.isValid(dto, this.context);
		assertThat(result).isTrue();
	}

}
