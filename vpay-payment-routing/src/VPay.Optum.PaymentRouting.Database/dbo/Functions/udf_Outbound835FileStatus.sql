-- =============================================
-- Author:		Jeff Hill
-- Create date: 02-22-2022
-- Description:	Check Constraints for Outbound835FileStatus SmartEnum
-- =============================================
CREATE FUNCTION [dbo].[udf_Outbound835FileStatus] 
(
    @StatusName VARCHAR(20)
)
RETURNS int
AS
BEGIN
    DECLARE @Result int
    SELECT @Result = CASE WHEN @StatusName IN (
            'QUEUED',
            'STARTED',
            'BUILT',
            'TRANSFERRING',
            'FAILEDSEND',
            'COMPLETED',
            'FinSegTxtQueued',
            'FinSeg835Queued',
            'FinSegTxtStarted',
            'FinSeg835Started',
            'FinSegTxtBuilt',
            'FinSeg835Built'
            ) THEN 1 
        ELSE 0 END
    RETURN @Result
END