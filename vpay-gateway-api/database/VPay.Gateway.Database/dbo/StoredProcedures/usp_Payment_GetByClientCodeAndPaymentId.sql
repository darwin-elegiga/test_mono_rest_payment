CREATE PROCEDURE [dbo].[usp_Payment_GetByClientCodeAndPaymentId]
    @ClientCode varchar(10),
    @ClientPaymentId varchar(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        Id
        , ClientCode
        , ClientPaymentId
        , LastSentStatus
        , CreatedDateTime
        , LastModifiedDateTime
        , CurrentBalance
    FROM
        dbo.Payment
    WHERE
        ClientCode = @ClientCode
        AND ClientPaymentId = @ClientPaymentId

END
