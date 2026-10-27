-- =============================================
-- Author:		Jeff Hill
-- Create date: 02-22-2022
-- Description:	Check Constraints for BatchStatus SmartEnum
-- =============================================
CREATE FUNCTION [dbo].[udf_BatchStatus] 
(
	@StatusName VARCHAR(20)
)
RETURNS int
AS
BEGIN
	DECLARE @Result int
	SELECT @Result = CASE WHEN @StatusName IN (
			'BatchInitiated',
			'BatchReceived',
			'BatchComplete',
			'BatchCancelled' ) THEN 1 
		ELSE 0 END
	RETURN @Result
END