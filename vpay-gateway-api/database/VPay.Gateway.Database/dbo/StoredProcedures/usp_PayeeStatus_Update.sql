CREATE PROCEDURE [dbo].[usp_PayeeStatus_Update]
    @PayeeStatusId bigint
   ,@Status varchar(50)
   ,@PreviousStatus varchar(50)
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY

        -- Update the payee status
        UPDATE
            [dbo].[PayeeStatus]
        SET
            [CurrentProcessStatus] = @Status
           ,[LastModifiedDateTime] = SYSDATETIME()
        WHERE
            [Id] = @PayeeStatusId
            AND [CurrentProcessStatus] = @PreviousStatus;
            
        -- if a record was inserted we also want to insert a status tracking record
        IF @@ROWCOUNT <> 0
        BEGIN
            INSERT INTO [dbo].[PayeeStatusTracking] (
                [PayeeStatusId]
               ,[Status]
            ) VALUES (
                @PayeeStatusId
               ,@Status
            );
   
            SELECT
                [Id]
               ,[PayeeId]
               ,[ClaimId]
               ,[PepPayeeId]
               ,[Status]
               ,[CurrentProcessStatus]
               ,[PepStatusId]
               ,[StatusDateTime]
               ,[CreatedDateTime]
               ,[LastModifiedDateTime]
               ,[PartyStatusId]
               ,[PartyStatus]
               ,[RetryCount]
            FROM
                [dbo].[PayeeStatus]
            WHERE
                [Id] = @PayeeStatusId;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH

END
GO
