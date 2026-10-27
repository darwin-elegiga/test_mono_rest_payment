CREATE PROCEDURE [dbo].[usp_GuidewireUnloadProcess_GetByReasonCode]
    @ReasonCode varchar(4)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        ReasonCode,
        ProcessType,
        Comment
    FROM
        [dbo].[GuidewireUnloadProcess]
    WHERE
        ReasonCode = @ReasonCode;
END
