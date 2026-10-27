package com.uhg.optum.financials.payments.payer.job.param;

import lombok.Getter;
import lombok.Setter;

import java.io.Serial;
import java.io.Serializable;

@Getter
@Setter
public class Payment835JobParam implements Serializable {

	@Serial
	private static final long serialVersionUID = 1L;

	private String jobId;

	private String eventType;

	private String correlationId;

	private String old835FileDate;

}
