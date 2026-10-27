CREATE PROCEDURE [dbo].[usp_Payment_GetByTransactionId]
    @TransactionId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        p.Id
        , p.ClientCode
        , p.ClientPaymentId
        , p.LastSentStatus
        , p.CreatedDateTime
        , p.LastModifiedDateTime
        , p.CurrentBalance
    FROM
        dbo.Payment AS p
            INNER JOIN dbo.PaymentEvent as evt ON p.Id = evt.PaymentId
    WHERE
        evt.TransactionId = @TransactionId
    GROUP BY
        p.Id
        , p.ClientCode
        , p.ClientPaymentId
        , p.LastSentStatus
        , p.CreatedDateTime
        , p.LastModifiedDateTime
        , p.CurrentBalance

END
GO