CREATE PROCEDURE [dbo].[usp_Outbound835File_GetByInboundPaymentId]
    @InboundPaymentId BIGINT
AS
BEGIN
    SET NOCOUNT ON

    SELECT  o.Id AS Id,
            o.PayerAlias AS PayerAlias,
            o.OutboundRefEO AS OutboundRefEo,
            o.[Status] AS [Status],
            o.FilePath AS FilePath,
            o.CreatedBy AS CreatedBy,
            o.CreatedDateTime AS CreatedDateTime,
            o.LastModifiedBy AS LastModifiedBy,
            o.LastModifiedDateTime AS LastModifiedDateTime,
            o.SentDateTime AS SentDateTime,
            s.CurrentSequenceNumber AS SequenceNumber,
            s.DateStamp
    FROM Outbound835File o
		JOIN Outbound835FileTransaction t ON t.Outbound835FileId = o.Id
		JOIN InboundPayment p ON p.Id = t.InboundPaymentId
        LEFT JOIN OutboundFinSegSequence s ON s.Id = o.OutboundFinSegSequenceId
    WHERE t.InboundPaymentId = @InboundPaymentId

    SELECT  p.Id
    FROM InboundPayment p
		JOIN Outbound835FileTransaction t ON p.Id = t.InboundPaymentId
    WHERE t.InboundPaymentId = @InboundPaymentId

END
