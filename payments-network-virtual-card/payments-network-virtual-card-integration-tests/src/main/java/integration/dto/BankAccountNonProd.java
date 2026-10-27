package integration.dto;

import lombok.Getter;
import lombok.Setter;

public class BankAccountNonProd {

	@Getter
	@Setter
	private String name;

	@Getter
	@Setter
	private String displayName;

	@Getter
	@Setter
	private String routingNumber;

	@Getter
	@Setter
	private String accountNumber;

	@Getter
	@Setter
	private String accountType;

}
