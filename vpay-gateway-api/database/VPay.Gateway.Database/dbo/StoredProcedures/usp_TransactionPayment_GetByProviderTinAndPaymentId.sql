--=============================================================================
--
-- GET TRANSACTION PAYMENT INFO BY PROVIDER TIN AND PAYMENT ID
-- 
--=============================================================================
-- Get single Transaction Payment record matching input parameters:
-- Provider Tin            : required.
-- Payment Id              : required.
--=============================================================================
CREATE OR ALTER   PROCEDURE [dbo].[usp_TransactionPayment_GetByProviderTinAndPaymentId]
	@ProviderTin	VARCHAR(50),
	@PaymentId		VARCHAR(100)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @TransactionId BIGINT
	SET @TransactionId = NULL;

	SELECT
		@TransactionId = b.SETXID
	FROM
		[Gateway].[VPAY01].[VPAYBRDDTA_BRD835MAP] b
	WHERE
		b.TAXID = @ProviderTin
		AND b.PaymentID = @PaymentId

	IF (@TransactionId IS NOT NULL)
	BEGIN
		EXEC [dbo].[usp_TransactionPayment_GetByTransactionId]
				@TransactionId = @TransactionId
	END

END
GO