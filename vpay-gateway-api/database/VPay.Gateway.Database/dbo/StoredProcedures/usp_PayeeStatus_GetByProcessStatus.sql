CREATE OR ALTER PROCEDURE [dbo].[usp_PayeeStatus_GetByProcessStatus]
    @CurrentProcessStatus VARCHAR(50) = NULL,
    @ClientCode VARCHAR(10) = NULL,
    @MemberId VARCHAR(20) = NULL,
    @ClaimId VARCHAR(32) = NULL,
    @Page INT = NULL,
    @PageSize INT = NULL
AS
BEGIN

    -- set default current page to first page
    IF (@Page IS NULL OR @Page < 1)
        SET @Page = 1;

    -- set default page size for the number of records to return
    IF (@PageSize IS NULL OR @PageSize <= 0)
        SET @PageSize = 50;

    SELECT		PS.[Id]
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
    FROM		[dbo].[PayeeStatus] PS
    JOIN        [dbo].[Payee] P ON P.[Id] = PS.[PayeeId]
    WHERE		(@CurrentProcessStatus IS NULL OR ([CurrentProcessStatus] = @CurrentProcessStatus))
                AND (@ClientCode IS NULL OR ([ClientCode] = @ClientCode))
                AND (@MemberId IS NULL OR ([MemberId] LIKE RTRIM(@MemberId) + '%'))
                AND (@ClaimId IS NULL OR ([ClaimId] LIKE RTRIM(@ClaimId) + '%'))
    ORDER BY	[CreatedDateTime]
    OFFSET      (@Page - 1) * @PageSize ROWS
    FETCH NEXT  @PageSize ROWS ONLY
    ;

    -- select pagination data for the results found above
    SELECT      @Page AS CurrentPage
            ,   @PageSize AS PageSize
            ,   COUNT(*) AS TotalRows
    FROM		[dbo].[PayeeStatus] PS
    JOIN        [dbo].[Payee] P ON P.[Id] = PS.[PayeeId]
    WHERE		(@CurrentProcessStatus IS NULL OR ([CurrentProcessStatus] = @CurrentProcessStatus))
                AND (@ClientCode IS NULL OR ([ClientCode] = @ClientCode))
                AND (@MemberId IS NULL OR ([MemberId] LIKE RTRIM(@MemberId) + '%'))
                AND (@ClaimId IS NULL OR ([ClaimId] LIKE RTRIM(@ClaimId) + '%'))
    ;
END
GO
