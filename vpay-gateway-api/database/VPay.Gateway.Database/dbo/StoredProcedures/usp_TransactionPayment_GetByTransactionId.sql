--=============================================================================
--
-- GET TRANSACTION PAYMENT INFO BY TRANSACTION ID
-- 
--=============================================================================
-- Get single Transaction Payment record matching input parameters:
-- Transaction Id            : required.
--=============================================================================
CREATE OR ALTER   PROCEDURE [dbo].[usp_TransactionPayment_GetByTransactionId]
      @TransactionId         BIGINT
AS
BEGIN
    SET NOCOUNT ON;	

    SELECT TOP 1
          T.[ID] as [Id]
        , T.[SETXID] as [TransactionId]
        , T.[TPA] as [ClientCode]
        , T.[BILENT] as [BillingEntityCode]
        , T.[PaymentID] as [PaymentId]
    FROM
        [dbo].[CPSTMF] T
    WHERE
         T.[SETXID] = @TransactionId
    ORDER BY 
         T.[REPDATE] DESC

END
GO
