-- =============================================
-- Author:		Jeff Hill
-- Create date: 02-22-2022
-- Description:	Check Constraints for PaymentStatus SmartEnum
-- =============================================
CREATE FUNCTION [dbo].[udf_InboundPaymentStatus] 
(
    @StatusName VARCHAR(20)
)
RETURNS int
AS
BEGIN
    DECLARE @Result int
    SELECT @Result = CASE WHEN @StatusName IN (
        'RECEIVED',
        'APSENT',
        'APSENTSKIP',
        'OUT835BUILT',
        'QUEUED835GEN',
        'FinSegBuilt',
        'FinSegQueued',
        'CANCELLED'
    ) THEN 1 
    ELSE 0 END
    RETURN @Result
END