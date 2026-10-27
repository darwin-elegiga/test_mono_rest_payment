
package com.uhg.optum.financials.payments.payer.dto;

import com.uhg.optum.financials.payments.payer.validation.AddressLineOneValidator;
import com.uhg.optum.financials.payments.payer.validation.CityValidator;
import com.uhg.optum.financials.payments.payer.validation.StateValidator;
import com.uhg.optum.financials.payments.payer.validation.ZipCodeValidator;
import jakarta.validation.constraints.Size;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.io.Serializable;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class AcquirerAddress implements Serializable {

	@AddressLineOneValidator
	@Size(max = 50, message = "Acquirer Address Line1 should be less than or equal to 50 characters")
	private String line1;

	@Size(max = 50, message = "Acquirer Address Line2 should be less than or equal to 50 characters")
	private String line2;

	@CityValidator
	@Size(max = 20, message = "Acquirer city should be less than or equal to 20 characters")
	private String city;

	@StateValidator
	private String state;

	@ZipCodeValidator
	private String zipCode;

	@Getter
	@Setter
	private String country;

}
