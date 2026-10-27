CREATE PROCEDURE [dbo].[usp_PaymentProcessQueue_Delete]
    @ClientCode varchar(10),
    @ClientPaymentId varchar(100)
AS
BEGIN
    DELETE FROM dbo.PaymentProcessQueue
    WHERE
        ClientCode = @ClientCode
        AND ClientPaymentId = @ClientPaymentId

    SELECT @@ROWCOUNT AS [RowCount]
END
