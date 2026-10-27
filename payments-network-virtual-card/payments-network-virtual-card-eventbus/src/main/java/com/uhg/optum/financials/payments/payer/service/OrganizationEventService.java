package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.organization.events.OrganizationClosed;
import com.uhg.optum.financials.payments.organization.events.OrganizationRegistered;
import com.uhg.optum.financials.payments.organization.events.OrganizationUpdated;

import java.util.UUID;

public interface OrganizationEventService {

	void addOrganizationDetails(OrganizationRegistered dto, UUID orgId);

	void updateOrganizationDetails(OrganizationUpdated dto, UUID orgId);

	void closeOrganizationDetails(OrganizationClosed dto, UUID orgId);

}
