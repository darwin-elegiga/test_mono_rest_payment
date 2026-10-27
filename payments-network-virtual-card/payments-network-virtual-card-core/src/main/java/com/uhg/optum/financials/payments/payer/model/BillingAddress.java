package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Entity
@Table(name = "billing_addresses", schema = "virtual_card")
@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class BillingAddress {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Long id;

	@Column(name = "line1", nullable = false, length = 255)
	private String line1;

	@Column(name = "city", nullable = false, length = 255)
	private String city;

	@Column(name = "country", nullable = false, length = 3)
	private String country;

	@Column(name = "postal_code", nullable = false, length = 255)
	private String postalCode;

}
