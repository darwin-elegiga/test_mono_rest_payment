CREATE OR ALTER PROCEDURE [dbo].[usp_PayeeStatus_UpdateProcessStatus]
    @CurrentProcessStatus VARCHAR(50),
    @NewProcessStatus VARCHAR(50),
    @ClientCode VARCHAR(10) = NULL,
    @MemberId VARCHAR(20) = NULL,
    @ClaimId VARCHAR(32) = NULL,
    @MessageCount INT = NULL,
    @MinMessageAgeInMinutes INT = NULL,
    @MaxMessageAgeInDays INT = NULL
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY

        -- set default maximum number of messages to be considered for the update
        IF (@MessageCount IS NULL OR @MessageCount < 1)
            SET @MessageCount = 1;

        -- set default minimum message age in minutes in the current status to be considered for the update
        IF (@MinMessageAgeInMinutes IS NULL OR @MinMessageAgeInMinutes < 1)
            SET @MinMessageAgeInMinutes = 30;

        -- set default maximum message age in days in the current status to be considered for the update
        IF (@MaxMessageAgeInDays IS NULL OR @MaxMessageAgeInDays < 1)
            SET @MaxMessageAgeInDays = 60;

        -- identify stuck messages for payee enrollments
        SELECT TOP (@MessageCount) 
                    PS.[Id]
                ,   P.[ClientCode]
                ,   P.[MemberId]
                ,   PS.[PayeeId]
                ,   PS.[ClaimId]
                ,   PS.[PepPayeeId]
                ,   PS.[Status]
                ,   PS.[CurrentProcessStatus]
                ,   PS.[PepStatusId]
                ,   PS.[PartyStatusId]
                ,   PS.[PartyStatus]
                ,   PS.[RetryCount]
                ,   PS.[StatusDateTime]
                ,   PS.[CreatedDateTime]
                ,   PS.[LastModifiedDateTime]
        INTO        #PayeeStuckMessages
        FROM        [dbo].[PayeeStatus] PS
        JOIN        [dbo].[Payee] P ON P.[Id] = PS.[PayeeId]
        WHERE       PS.[CurrentProcessStatus] = @CurrentProcessStatus
                    AND (PS.[LastModifiedDateTime] < DATEADD(MINUTE, -@MinMessageAgeInMinutes, GETDATE()))
                    AND (PS.[LastModifiedDateTime] >= DATEADD(DAY, -@MaxMessageAgeInDays, GETDATE()))
                    AND (@ClientCode IS NULL OR (P.[ClientCode] = @ClientCode))
                    AND (@MemberId IS NULL OR (P.[MemberId] = @MemberId))
                    AND (@ClaimId IS NULL OR (PS.[ClaimId] = @ClaimId))
                    AND (ISNULL(PS.[RetryCount],0) < 6)
        ORDER BY    PS.[CreatedDateTime]
        ;

        -- update the processing status for the results found above
        UPDATE      [dbo].[PayeeStatus] SET
                    [CurrentProcessStatus] = @NewProcessStatus,
                    [RetryCount] = ISNULL([RetryCount], 0) + 1,
                    [LastModifiedDateTime] = SYSDATETIME()
        FROM        [dbo].[PayeeStatus] PS
        WHERE       EXISTS (SELECT 1 FROM #PayeeStuckMessages PSM WHERE PSM.[Id] = PS.[Id])
        ;

        -- insert a new status change record for the results found above
        INSERT INTO [dbo].[PayeeStatusTracking] 
        (
                    [PayeeStatusId]
                ,   [Status]
        ) 
        SELECT      [Id]
                ,   @NewProcessStatus
        FROM        #PayeeStuckMessages
        ;

        -- return the list of affected records
        SELECT * FROM #PayeeStuckMessages
        ;
        DROP TABLE IF EXISTS #PayeeStuckMessages
        ;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
GO
