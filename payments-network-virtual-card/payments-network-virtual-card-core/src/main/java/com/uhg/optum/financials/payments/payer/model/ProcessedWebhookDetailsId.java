package com.uhg.optum.financials.payments.payer.model;

import java.io.Serializable;
import java.util.Objects;

public class ProcessedWebhookDetailsId implements Serializable {

	private String id;

	private String eventType;

	private String processedStatus;

	public ProcessedWebhookDetailsId() {
	}

	public ProcessedWebhookDetailsId(String id, String eventType, String processedStatus) {
		this.id = id;
		this.eventType = eventType;
		this.processedStatus = processedStatus;
	}

	public String getId() {
		return this.id;
	}

	public void setId(String id) {
		this.id = id;
	}

	public String getEventType() {
		return this.eventType;
	}

	public void setEventType(String eventType) {
		this.eventType = eventType;
	}

	public String getProcessedStatus() {
		return this.processedStatus;
	}

	public void setProcessedStatus(String processedStatus) {
		this.processedStatus = processedStatus;
	}

	@Override
	public boolean equals(Object other) {
		if (this == other) {
			return true;
		}
		if (!(other instanceof ProcessedWebhookDetailsId that)) {
			return false;
		}
		return Objects.equals(this.id, that.id) && Objects.equals(this.eventType, that.eventType)
				&& Objects.equals(this.processedStatus, that.processedStatus);
	}

	@Override
	public int hashCode() {
		return Objects.hash(this.id, this.eventType, this.processedStatus);
	}

}
