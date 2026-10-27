package com.uhg.optum.financials.payments.payer.dto.organization;

import com.uhg.optum.financials.payments.organization.enums.FundingMethod;
import com.uhg.optum.financials.payments.organization.enums.MarketType;
import com.uhg.optum.financials.payments.organization.models.Address;
import com.uhg.optum.financials.payments.organization.models.Contact;
import com.uhg.optum.financials.payments.organization.models.SecondaryContact;
import io.swagger.v3.oas.annotations.media.Schema;
import lombok.Getter;
import lombok.Setter;

import java.util.Date;

@Getter
@Setter
public class OrganizationDTO {

	@Getter
	@Setter
	@Schema(example = "Medical")
	private MarketType marketType;

	@Getter
	@Setter
	private String shortCode;

	@Getter
	@Setter
	private String name;

	@Getter
	@Setter
	@Schema(example = "980745362")

	private String tin;

	@Getter
	@Setter
	private Date onboardedOn;

	@Getter
	@Setter
	@Schema(example = "NonFunded")
	private FundingMethod fundingMethod;

	@Getter
	@Setter
	@Schema(example = "HEALTHCARE")

	private String programName;

	@Getter
	@Setter
	private Address address;

	@Getter
	@Setter
	private Contact contact;

	@Getter
	@Setter
	private SecondaryContact claimOfficeContact;

	@Getter
	@Setter
	private SecondaryContact technicalContact;

	@Getter
	@Setter
	private String createdBy;

	@Getter
	@Setter
	@Schema(example = "organization_registered")
	private String type;

}
