CREATE PROCEDURE [dbo].[usp_PayeeStatus_Insert]
   @PayeeId bigint
  ,@ClaimId varchar(32)
  ,@PepPayeeId bigint
  ,@Status varchar(50)
  ,@CurrentProcessStatus varchar(50)
  ,@PepStatusId bigint
  ,@StatusDateTime DATETIME2
  ,@PartyStatusId bigint = NULL
  ,@PartyStatus varchar(50) = NULL
  ,@RetryCount int = 0
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY

        INSERT INTO [dbo].[PayeeStatus] (
            [PayeeId]
           ,[ClaimId]
           ,[PepPayeeId]
           ,[Status]
           ,[CurrentProcessStatus]
           ,[PepStatusId]
           ,[StatusDateTime]
           ,[PartyStatusId]
           ,[PartyStatus]
           ,[RetryCount]
        ) VALUES (
            @PayeeId
           ,@ClaimId
           ,@PepPayeeId
           ,@Status
           ,@CurrentProcessStatus
           ,@PepStatusId
           ,@StatusDateTime
           ,@PartyStatusId
           ,@PartyStatus
           ,@RetryCount
        );

        DECLARE @PayeeStatusId BIGINT = SCOPE_IDENTITY();
        INSERT INTO [dbo].[PayeeStatusTracking] (
            [PayeeStatusId]
           ,[Status]
        ) VALUES (
            @PayeeStatusId
           ,@CurrentProcessStatus
        );
   
        EXEC [dbo].[usp_PayeeStatus_GetByPayeeIdAndClaimIdAndStatusAndPartyStatus]
             @PayeeId
            ,@ClaimId
            ,@Status
            ,@PartyStatus

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH

END
GO
