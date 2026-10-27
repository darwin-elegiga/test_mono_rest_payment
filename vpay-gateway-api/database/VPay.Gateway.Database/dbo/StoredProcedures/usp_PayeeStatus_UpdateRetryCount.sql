CREATE PROCEDURE [dbo].[usp_PayeeStatus_UpdateRetryCount]
    @ClientCode VARCHAR(10),
    @MemberId VARCHAR(20),
    @ClaimId VARCHAR(32),    
    @RetryCount int = 0
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY

        DECLARE @PayeeStatusId bigint = 0;

        -- Check if the payee status exists for the given client code, member id, and claim id
        SELECT TOP 1 
            @PayeeStatusId = PS.[Id] 
        FROM [dbo].[PayeeStatus] PS 
            JOIN [dbo].[Payee] P ON P.[Id] = PS.[PayeeId] 
        WHERE P.ClientCode = @ClientCode
            AND P.MemberId = @MemberId   
            AND PS.ClaimId = @ClaimId

        -- Update the payee retry count
        UPDATE
            [dbo].[PayeeStatus]
        SET
            [RetryCount] = ISNULL(@RetryCount,0)
           ,[LastModifiedDateTime] = SYSDATETIME()
        WHERE
            [Id] = @PayeeStatusId;
            
        -- if a record was updated return record
        IF @@ROWCOUNT <> 0
        BEGIN 
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
