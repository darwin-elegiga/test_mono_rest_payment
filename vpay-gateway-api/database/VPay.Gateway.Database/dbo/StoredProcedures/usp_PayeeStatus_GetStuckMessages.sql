CREATE PROCEDURE [dbo].[usp_PayeeStatus_GetStuckMessages]
    @CurrentProcessStatus VARCHAR(50),
    @ClientCode VARCHAR(10) = NULL,
    @MemberId VARCHAR(20) = NULL,
    @ClaimId VARCHAR(32) = NULL,
    @MinMessageAgeInMinutes INT = NULL,
    @MaxMessageAgeInDays INT = NULL,
    @Page INT = NULL,
    @PageSize INT = NULL
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY

        -- set default minimum message age in minutes in the current status to be considered for the update
        IF (@MinMessageAgeInMinutes IS NULL OR @MinMessageAgeInMinutes < 1)
            SET @MinMessageAgeInMinutes = 30;

        -- set default maximum message age in days in the current status to be considered for the update
        IF (@MaxMessageAgeInDays IS NULL OR @MaxMessageAgeInDays < 1)
            SET @MaxMessageAgeInDays = 60;

        -- set default current page to first page
        IF (@Page IS NULL OR @Page < 1)
            SET @Page = 1;

        -- set default page size for the number of records to return
        IF (@PageSize IS NULL OR @PageSize <= 0)
            SET @PageSize = 50;

        -- identify stuck messages for payee enrollments
        SELECT
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
        OFFSET      (@Page - 1) * @PageSize ROWS
        FETCH NEXT  @PageSize ROWS ONLY
        ;

        -- select pagination data for the results found above
        SELECT      @Page AS CurrentPage
                ,   @PageSize AS PageSize
                ,   COUNT(*) AS TotalRows
        FROM        [dbo].[PayeeStatus] PS
        JOIN        [dbo].[Payee] P ON P.[Id] = PS.[PayeeId]
        WHERE       PS.[CurrentProcessStatus] = @CurrentProcessStatus
                    AND (PS.[LastModifiedDateTime] < DATEADD(MINUTE, -@MinMessageAgeInMinutes, GETDATE()))
                    AND (PS.[LastModifiedDateTime] >= DATEADD(DAY, -@MaxMessageAgeInDays, GETDATE()))
                    AND (@ClientCode IS NULL OR (P.[ClientCode] = @ClientCode))
                    AND (@MemberId IS NULL OR (P.[MemberId] = @MemberId))
                    AND (@ClaimId IS NULL OR (PS.[ClaimId] = @ClaimId))
                    AND (ISNULL(PS.[RetryCount],0) < 6)
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
