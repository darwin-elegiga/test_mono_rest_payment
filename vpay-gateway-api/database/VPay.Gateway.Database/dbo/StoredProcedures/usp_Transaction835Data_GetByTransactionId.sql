--=============================================================================
--
-- GET TRANSACTION BOARDING 835 INFO BY TRANSACTION ID
-- 
--=============================================================================
-- Get single Boarding 835 record matching input parameters:
-- Transaction Id            : required.
--=============================================================================
CREATE OR ALTER   PROCEDURE [dbo].[usp_Transaction835Data_GetByTransactionId]
      @TransactionId         BIGINT
AS
BEGIN
    SET NOCOUNT ON;	

    SELECT TOP 1
          T.[ID] as [Id]
        , T.[TransactionID] as [TransactionId]
        , T.[TRN] as [TraceNumber]
        , T.[REPDATE] as [LastModifiedDate]        
    FROM
        [dbo].[BOARDING_835DATA] T
    WHERE
         T.TransactionID = @TransactionId
    ORDER BY 
         T.[REPDATE] DESC

END
GO
