CREATE PROCEDURE [dbo].[usp_PaymentStatusTracking_GetByPaymentId]
    @PaymentId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id
       ,PaymentId
       ,[Status]
       ,CreatedDateTime
    FROM
        [dbo].[PaymentStatusTracking]
    WHERE
        PaymentId = @PaymentId;

END
GO
