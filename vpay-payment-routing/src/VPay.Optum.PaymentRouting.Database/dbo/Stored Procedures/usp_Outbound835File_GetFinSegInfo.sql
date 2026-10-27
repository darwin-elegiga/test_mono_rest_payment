CREATE PROCEDURE [dbo].[usp_Outbound835File_GetFinSegInfo]
    @Outbound835FileId bigint
AS
SET NOCOUNT ON;

SELECT
    o.PayerAlias AS PayerAlias,
    o.[Status] AS [Status],
    o.CreatedDateTime AS CreatedDateTime,
    SUM(p.PaymentAmount) AS PaymentAmount,
    LEFT(RIGHT(a.TrnSegment, 6), 5) AS PayerId,
    f.DateStamp,
    f.[CurrentSequenceNumber] AS SequenceNumber
FROM [dbo].[Outbound835File] AS o
    JOIN [dbo].[Outbound835FileTransaction] AS t ON t.Outbound835FileId = o.Id
    JOIN [dbo].[OutboundFinSegSequence] AS f ON f.Id = o.OutboundFinSegSequenceId
    JOIN [dbo].[InboundPayment] AS p ON p.Id = t.InboundPaymentId
    JOIN [dbo].[ApMessageTracking] AS a ON a.InboundPaymentId = p.Id
WHERE o.Id = @Outbound835FileId
GROUP BY
    o.PayerAlias,
    o.[Status],
    o.CreatedDateTime,
    LEFT(RIGHT(a.TrnSegment, 6), 5),
    f.DateStamp,
    f.[CurrentSequenceNumber]
