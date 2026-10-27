--=============================================================================
--
-- GET TRANSACTION NETWORK INFO BY TRANSACTION ID
-- 
--=============================================================================
-- Get single Transaction Network record matching input parameters:
-- Transaction Id            : required.
--=============================================================================
CREATE OR ALTER   PROCEDURE [dbo].[usp_TransactionNetwork_GetByTransactionId]
      @TransactionId         BIGINT
AS
BEGIN
    SET NOCOUNT ON;	

    SELECT TOP 1
          T.[ID] as [Id]
        , T.[SETXID] as [TransactionId]
        , T.[NETWORK_NAME] as [NetworkName]
        , T.[NETWORK_ENROLLMENT_T] as [EnrollmentType]
        , T.[NETWORK_TYPE] as [NetworkType]
    FROM
        [dbo].[VPTMFCTR] T
    WHERE
         T.[SETXID] = @TransactionId
    ORDER BY 
         T.[REPDATE] DESC

END
GO
