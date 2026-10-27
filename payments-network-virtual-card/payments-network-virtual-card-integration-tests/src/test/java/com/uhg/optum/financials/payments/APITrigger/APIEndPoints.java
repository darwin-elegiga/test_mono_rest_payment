package com.uhg.optum.financials.payments.APITrigger;

import com.uhg.optum.financials.payments.payer.constant.FileType;
import okhttp3.RequestBody;
import okhttp3.ResponseBody;
import retrofit2.Call;
import retrofit2.http.*;

import java.util.UUID;

public interface APIEndPoints {

	@POST("/api/virtual-card/v1/payment/load")
	Call<ResponseBody> loadPayment(@Body RequestBody Reqbody);

	@GET("api/virtual-card/v1/payment/{paymentID}")
	Call<ResponseBody> getPaymentDetails(@Path("paymentID") String paymentID);

	@POST("/api/mock/virtual-card/v1/accounts/register")
	Call<ResponseBody> loadAccount(@Body RequestBody Reqbody);

	@GET("/api/virtual-card/v1/payment/get-files/{correlationId}")
	Call<ResponseBody> getPaymentFiles(@Path("correlationId") String correlationId);

	@POST("/api/org/v2/organizations")
	Call<ResponseBody> loadOrganizationNonProd(@Header("X-Optum-Tenant-Id") String X_Optum_Tenant_Id,
			@Body RequestBody reqBody);

	@POST("/api/org/v1/organizations/accounts")
	Call<ResponseBody> loadAccountNonProd(@Header("X-Optum-Tenant-Id") String X_Optum_Tenant_Id,
			@Body RequestBody reqBody);

	@POST("/api/org/v1/organizations/accounts/{accountShortCode}/subs")
	Call<ResponseBody> loadSubAccountNonProd(@Header("X-Optum-Tenant-Id") String X_Optum_Tenant_Id,
			@Path("accountShortCode") String accountShortCode, @Body RequestBody reqBody);

	@POST("/api/mock/virtual-card/v1/sub-accounts/register")
	Call<ResponseBody> loadSubAccount(@Body RequestBody Reqbody);

	@POST("/api/mock/virtual-card/v1/organizations/register")
	Call<ResponseBody> loadOrganization(@Body RequestBody Reqbody);

	@GET("api/mock/virtual-card/v1/organizations/{orgId}")
	Call<ResponseBody> getOrganizationDetails(@Path("orgId") String orgId);

	@POST("/api/mock/virtual-card/v1/sub-accounts/update")
	Call<ResponseBody> loadSubAccountUpdated(@Body RequestBody Reqbody);

	@POST("/api/mock/virtual-card/v1/accounts/produce/logo-updated-event")
	Call<ResponseBody> loadLogoUpdatedEvent(@Body RequestBody reqBody);

	@POST("/api/mock/virtual-card/v1/logo-details")
	Call<ResponseBody> getLogoDetails(@Body RequestBody reqBody);

	@PATCH("/api/org/v1/organizations/accounts/{accountShortCode}/subs/{subAccountShortCode}")
	Call<ResponseBody> loadSubAccountUpdatedNonProd(@Header("X-Optum-Tenant-Id") String X_Optum_Tenant_Id,
			@Path("accountShortCode") String accountShortCode, @Path("subAccountShortCode") String subAccountShortCode,
			@Body RequestBody reqBody);

	@POST("/api/mock/virtual-card/v1/accounts/produce/bank-account/details")
	Call<ResponseBody> loadBankAccount(@Body RequestBody reqBody);

	@GET("/api/virtual-card/v1/creation-details/{requestId}")
	Call<ResponseBody> getCardCreationDetails(@Path("requestId") String requestId);

	@GET("/api/virtual-card/v1/card-polling/{paymentId}")
	Call<ResponseBody> getCardPollingDetails(@Path("paymentId") String paymentId);

	@POST("/api/virtual-card/v1/spend-rules/configure")
	Call<ResponseBody> configureSpendRules(@Body RequestBody Reqbody, @Header("tenantId") String tenantId);

	@POST("/api/virtual-card/cards/webhooks")
	Call<ResponseBody> createCallBack(@Body RequestBody Reqbody);

	@GET("/api/virtual-card/v1/virtual-card/{paymentID}")
	Call<ResponseBody> getVirtualCardDetails(@Path("paymentID") String requestID);

	@POST("/api/virtual-card/v1/cancel-card")
	Call<ResponseBody> cancelCardRequest(@Body RequestBody Reqbody);

	@POST("/api/mock/virtual-card/v1/webhook/event")
	Call<ResponseBody> createIssuingWebhook(@Body RequestBody Reqbody);

	@GET("/api/virtual-card/v1/cancel/{paymentId}")
	Call<ResponseBody> getCancelCardDetails(@Path("paymentId") String paymentId);

	@GET("/api/virtual-card/v1/card-reset/{receiptId}")
	Call<ResponseBody> getCardResetDetails(@Path("receiptId") String receiptId);

	@GET("/api/mock/virtual-card/v1/card/get-auth-extract-record")
	Call<ResponseBody> getAuthExtractRecord(@Query("receiptId") String receiptId);

	@POST("/api/mock/virtual-card/v1/card/upload/generate-clear-extract/by-date-range")
	Call<ResponseBody> generateClearExtractByDateRange(@Query("startDate") String startDate);

	@POST("/api/virtual-card/v1/reset-card")
	Call<ResponseBody> resetCardRequest(@Body RequestBody Reqbody);

	@POST("/api/mock/virtual-card/v1/card/generate-auth-events")
	Call<ResponseBody> generateAuthEvents(@Query("startDate") String startDate,
			@Query("numberOfAuthEvents") int numberOfAuthEvents, @Query("cguid") String cardId);

	@POST("/api/mock/virtual-card/v1/produce/batch/event")
	Call<ResponseBody> publishBatchEvent(@Body RequestBody Reqbody);

	@GET("/api/virtual-card/v1/batch/get/payment-count-and-amount-by-status/{correlationId}")
	Call<ResponseBody> getBatchEvent(@Path("correlationId") String correlationId);

	@POST("/api/bank-account/v1/bank-accounts")
	Call<ResponseBody> loadBankAccountNonProd(@Header("X-Optum-Tenant-Id") String X_Optum_Tenant_Id,
			@Body RequestBody reqBody);

	@GET("/api/virtual-card/v1/accounts/get/bank-account/{bankAccountId}")
	Call<ResponseBody> getBankAccountDetails(@Path("bankAccountId") UUID bankAccountId);

	@GET("/api/virtual-card/v1/accounts/get/account/{orgShortCode}/{accountShortCode}")
	Call<ResponseBody> getAccountDetails(@Path("orgShortCode") String orgShortCode,
			@Path("accountShortCode") String accountShortCode);

	@GET("/api/virtual-card/v1/accounts/get/sub-account/{orgShortCode}/{accountShortCode}/{subAccountShortCode}")
	Call<ResponseBody> getSubAccountDetails(@Path("orgShortCode") String orgShortCode,
			@Path("accountShortCode") String accountShortCode, @Path("subAccountShortCode") String subAccountShortCode);

	@GET("/api/virtual-card/v1/accounts/get/organization/{orgShortCode}")
	Call<ResponseBody> getOrganizationDetailsInVC(@Path("orgShortCode") String orgShortCode);

	@GET("/api/org/v1/bank-accounts/{id}")
	Call<ResponseBody> getBankAccountDetailsFromOrg(@Path("id") UUID bankAccountId,
			@Header("X-Optum-Tenant-Id") String X_Optum_Tenant_Id);

}
