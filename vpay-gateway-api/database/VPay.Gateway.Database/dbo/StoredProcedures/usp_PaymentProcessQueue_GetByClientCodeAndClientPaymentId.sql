CREATE PROCEDURE [dbo].[usp_PaymentProcessQueue_GetByClientCodeAndClientPaymentId]
    @ClientCode varchar(10),
    @ClientPaymentId varchar(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id
        , ClientCode
        , ClientPaymentId
        , CreatedDateTime
        , LastRequestedDateTime
        , SYSDATETIME() AS CurrentDatabaseTime
    FROM
        [dbo].[PaymentProcessQueue]
    WHERE 
        ClientCode = @ClientCode AND ClientPaymentId = @ClientPaymentId    

END
GO
