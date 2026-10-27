package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.Address;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface AddressRepository extends JpaRepository<Address, Long> {

	Address findByLine1AndLine2AndCityAndStateAndPostalCode(String line1, String line2, String city, String state,
			String postalCode);

	Optional<Address> findById(Long addressId);

}
