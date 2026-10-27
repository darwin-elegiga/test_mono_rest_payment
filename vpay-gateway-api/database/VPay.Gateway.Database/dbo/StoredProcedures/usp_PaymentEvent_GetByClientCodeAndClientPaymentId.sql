CREATE PROCEDURE [dbo].[usp_PaymentEvent_GetByClientCodeAndClientPaymentId]
    @ClientCode varchar(10),
    @ClientPaymentId varchar(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.Id
        ,e.PaymentId
        ,e.TransactionId
        ,e.TransactionEventId
        ,e.EventStatus
        ,e.EventDateTime
        ,e.EventReasonCode
        ,e.EventReasonDescription
        ,e.CurrentStatus
        ,e.CreatedDateTime
        ,e.LastModifiedDateTime
        ,e.EventPaymentType
        ,e.EventTrackingNumber
    FROM
        [dbo].[PaymentEvent] AS e
        INNER JOIN [dbo].[Payment] AS p ON e.PaymentId = p.Id
    WHERE
        p.ClientCode = @ClientCode
        AND p.ClientPaymentId = @ClientPaymentId
END
