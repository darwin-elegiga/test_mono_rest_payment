package com.uhg.optum.financials.payments.payer.constant;

import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;

class AppConstantsTests {

	@Test
	void allConstants_shouldHaveExpectedValues() {
		assertThat(AppConstants.ECC_PUBLIC_KEY_KEY).isEqualTo("ecc-encryption-public-key");
		assertThat(AppConstants.ECC_PRIVATE_KEY_KEY).isEqualTo("ecc-encryption-private-key");
		assertThat(AppConstants.INFO).isEqualTo("info");
		assertThat(AppConstants.ERROR).isEqualTo("error");
		assertThat(AppConstants.DEBUG).isEqualTo("debug");
		assertThat(AppConstants.ERROR_PRIORITY).isEqualTo("errorPriority");
		assertThat(AppConstants.NAME).isEqualTo("name");
		assertThat(AppConstants.STATUS).isEqualTo("status");
		assertThat(AppConstants.VIRTUAL_CARD).isEqualTo("VIRTUAL_CARD");
		assertThat(AppConstants.VIRTUALCARD).isEqualTo("VIRTUALCARD");
		assertThat(AppConstants.RS1_0001).isEqualTo("RS1-0001");
		assertThat(AppConstants.RS2_0004).isEqualTo("RS2-0004");
		assertThat(AppConstants.BIN_654231).isEqualTo(654231);
		assertThat(AppConstants.CARD_CREATION_STATUS_MESSAGE).isEqualTo("Card creation status set to: ");
		assertThat(AppConstants.CARD_CANCELLATION_STATUS_MESSAGE).isEqualTo("Card cancellation status set to: ");
		assertThat(AppConstants.CARD_RESET_STATUS_MESSAGE).isEqualTo("Card reset status set to: ");
		assertThat(AppConstants.INVOKING_WEBHOOK_ENDPOINT).isEqualTo("Invoking webhook endpoint: ");
		assertThat(AppConstants.WITH_TENANT_ID).isEqualTo(" with tenantId: ");
		assertThat(AppConstants.VIRTUAL_CARD_CREATION_REQUEST_FAILED).isEqualTo("VIRTUAL_CARD_CREATION_REQUEST_FAILED");
		assertThat(AppConstants.CARD_ISSUE_FAILED).isEqualTo("CARD_ISSUE_FAILED");
		assertThat(AppConstants.CARD_CANCELLATION_FAILED).isEqualTo("CARD_CANCELLATION_FAILED");
		assertThat(AppConstants.AUTH_CLEAR_FAILED).isEqualTo("AUTH_CLEAR_FAILED");
		assertThat(AppConstants.UNKNOWN_CARD).isEqualTo("Unknown Card");
		assertThat(AppConstants.VOID_FAILED).isEqualTo("VOID_FAILED");
		assertThat(AppConstants.VOIDED).isEqualTo("voided");
		assertThat(AppConstants.ZETA_JID).isEqualTo("zeta-jid");
		assertThat(AppConstants.ZETA_CLIENT_ID).isEqualTo("zeta-client-id");
		assertThat(AppConstants.ZETA_CLIENT_SECRET).isEqualTo("zeta-client-secret");
		assertThat(AppConstants.ZETA_SETUP_PRIVATE_KEY).isEqualTo("zeta-setup-private-key");
		assertThat(AppConstants.ZETA_SETUP_PUBLIC_KEY).isEqualTo("zeta-setup-public-key");
		assertThat(AppConstants.TLS_PREMASTER_SECRET).isEqualTo("TlsPremasterSecret");
		assertThat(AppConstants.ECDH).isEqualTo("ECDH");
		assertThat(AppConstants.EC).isEqualTo("EC");
		assertThat(AppConstants.SECP256R1).isEqualTo("secp256r1");
		assertThat(AppConstants.VIRTUAL_CARD_CANCELLATION_REQUEST_FAILED)
			.isEqualTo("VIRTUAL_CARD_CANCELLATION_REQUEST_FAILED");
		assertThat(AppConstants.UNEXPECTED_RESPONSE_STATUS).isEqualTo("unexpected response status");
		assertThat(AppConstants.CANCELLATION_ERROR_MSG).isEqualTo("Error during card cancellation:");
		assertThat(AppConstants.AUTH_FILE_REGEX)
			.isEqualTo("^Card_Authorisation_Extract_\\d{2}-[A-Za-z]{3}-\\d{4}_\\d{2}_\\d{2}_\\d{2}\\.csv$");
		assertThat(AppConstants.CLEAR_FILE_REGEX)
			.isEqualTo("^Clearance_Extract_\\d{2}-[A-Za-z]{3}-\\d{4}_\\d{2}_\\d{2}_\\d{2}\\.csv$");
		assertThat(AppConstants.INVALID_CARD_DECLINE_FILE_REGEX)
			.isEqualTo("^Invalid_Card_Decline_Auth_Extract_\\d{2}-[A-Za-z]{3}-\\d{4}_\\d{2}_\\d{2}_\\d{2}\\.csv$");
		assertThat(AppConstants.PAYMENT_MSG_EXCEPTION_LOG)
			.isEqualTo("Exception while sending the payment message to the queue: ");
		assertThat(AppConstants.CREATE_CARD_MSG_EXCEPTION_LOG)
			.isEqualTo("Exception while sending the create virtual card message to the queue :,");
		assertThat(AppConstants.URL).isEqualTo("https://example.com/document.pdf");
		assertThat(AppConstants.CARD).isEqualTo("card");
		assertThat(AppConstants.ACTIVE).isEqualTo("Active");
		assertThat(AppConstants.DEBIT).isEqualTo("Debit");
		assertThat(AppConstants.FOR).isEqualTo("for:");
		assertThat(AppConstants.CLEAR_FILE_EXTRACT_PROCESSOR).isEqualTo("CLEAR_FILE_EXTRACT_PROCESSOR");
		assertThat(AppConstants.AUTH_FILE_EXTRACT_PROCESSOR).isEqualTo("AUTH_FILE_EXTRACT_PROCESSOR");
		assertThat(AppConstants.INVALID_CARD_DECLINE_AUTH_EXTRACT_PROCESSOR)
			.isEqualTo("INVALID_CARD_DECLINE_EXTRACT_PROCESSOR");
		assertThat(AppConstants.CLEAR).isEqualTo("CLEAR");
		assertThat(AppConstants.AUTH).isEqualTo("AUTH");
		assertThat(AppConstants.VIRTUAL_CARD_RESET_REQUEST_FAILED).isEqualTo("VIRTUAL_CARD_RESET_REQUEST_FAILED");
		assertThat(AppConstants.RESET_ERROR_MSG).isEqualTo("Error during reset card:");
		assertThat(AppConstants.CARD_GUID).isEqualTo("{cardGuid}");
		assertThat(AppConstants.CREDIT).isEqualTo("Credit");
		assertThat(AppConstants.UNABLE_TO_RESET).isEqualTo("Unable to Reset");
		assertThat(AppConstants.CARD_AUTH_REVERSED).isEqualTo("card.auth.reversed");
		assertThat(AppConstants.CARD_AUTH_REVERSED_FAILED).isEqualTo("card.auth.reversed.failed");
		assertThat(AppConstants.VIRTUAL_CARD_NOT_FOUND_ERROR).isEqualTo("VirtualCard not found for cardGuid:");
		assertThat(AppConstants.PAYMENT_DETAILS_NOT_FOUND_ERROR).isEqualTo("PaymentDetails not found for id:");
		assertThat(AppConstants.ERROR_MESSAGE).isEqualTo("with error:");
		assertThat(AppConstants.BATCH_DETAIL_STATUS_INITIALIZE).isEqualTo("P");
		assertThat(AppConstants.BATCH_DETAIL_STATUS_835_CREATED).isEqualTo("R");
		assertThat(AppConstants.BATCH_DETAIL_STATUS_COMPANION_CREATED).isEqualTo("C");
		assertThat(AppConstants.BATCH_DETAIL_STATUS_FAILED).isEqualTo("F");
		assertThat(AppConstants.BATCH_DETAIL_STATUS_PROGRESS).isEqualTo("I");
		assertThat(AppConstants.FUNDING_FILE).isEqualTo("835FINSEG");
		assertThat(AppConstants.OPTUM).isEqualTo("OPTUM");
		assertThat(AppConstants.OPTUMPAY_NETWORK).isEqualTo("OPTUMPAY");
		assertThat(AppConstants.TOKEN_CACHE_NAME).isEqualTo("tokenCache");
		assertThat(AppConstants.TOKEN_CACHE_KEY).isEqualTo("'zetaToken'");
		assertThat(AppConstants.ST_SE_EX).isEqualTo("ST*835*ST_INDEX_PLACEHOLDER~");
		assertThat(AppConstants.DEFAULT_HAS_CUSTOM_LOGO).isFalse();
		assertThat(AppConstants.CARD_AUTH_EVENT_APPROVED).isEqualTo("card.auth.approved");
		assertThat(AppConstants.CARD_AUTH_EVENT_DECLINED).isEqualTo("card.auth.declined");
		assertThat(AppConstants.ERROR_CONVERTING_DTO_TO_JSON).isEqualTo("Error converting DTO to JSON: ");
		assertThat(AppConstants.OTHER).isEqualTo("OTHER");
	}

}
