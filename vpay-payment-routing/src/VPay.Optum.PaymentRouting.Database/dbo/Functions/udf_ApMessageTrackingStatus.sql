

-- =============================================
-- Author:		Jeff Hill
-- Create date: 02-22-2022
-- Description:	Check Constraints for ApMessageTrackingStatus SmartEnum
-- =============================================
CREATE FUNCTION [dbo].[udf_ApMessageTrackingStatus] 
(
	@StatusName VARCHAR(20)
)
RETURNS int
AS
BEGIN
	DECLARE @Result int
	SELECT @Result = CASE WHEN @StatusName IN (
			'QUEUED',
			'APSENT',
			'START835BUILD',
			'FinSegNeeded',
			'StartFinSegBuild',
			'COMPLETED' ) THEN 1 
		ELSE 0 END
	RETURN @Result
END