CREATE PROCEDURE [dbo].[usp_PaymentEvent_GetByPaymentIdAndTransactionEventId]
    @PaymentId BIGINT,
    @TransactionEventId BIGINT
AS
BEGIN

    SELECT 
        Id
        ,PaymentId
        ,TransactionId
        ,TransactionEventId
        ,EventStatus
        ,EventDateTime
        ,EventReasonCode
        ,EventReasonDescription
        ,CurrentStatus
        ,CreatedDateTime
        ,LastModifiedDateTime
        ,EventPaymentType
        ,EventTrackingNumber
    FROM
        [dbo].[PaymentEvent]
    WHERE
        PaymentId = @PaymentId
        AND TransactionEventId = @TransactionEventId
END
