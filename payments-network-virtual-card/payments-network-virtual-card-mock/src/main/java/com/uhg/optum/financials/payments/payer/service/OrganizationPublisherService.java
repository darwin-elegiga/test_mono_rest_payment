package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationUpdatedDto;
import com.uhg.optum.financials.payments.payer.model.events.Organization;

public interface OrganizationPublisherService {

	String publishOrganizationRegisteredEvent(OrganizationRegisteredEvent organizationRegisteredEvent);

	String publishOrganizationUpdatedEvent(OrganizationUpdatedDto organizationUpdatedDto);

	Organization getOrgRegDetails(String orgId);

}
