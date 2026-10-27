CREATE PROCEDURE [dbo].[usp_Outbound835File_GetById]
    @Outbound835FileId bigint
AS
BEGIN
    SET NOCOUNT ON

    select  o.Id as Id,
            o.PayerAlias as PayerAlias,
            o.OutboundRefEO as OutboundRefEo,
            o.[Status] as [Status],
            o.FilePath as FilePath,
            o.CreatedBy as CreatedBy,
            o.CreatedDateTime as CreatedDateTime,
            o.LastModifiedBy as LastModifiedBy,
            o.LastModifiedDateTime as LastModifiedDateTime,
            o.SentDateTime as SentDateTime,
            s.CurrentSequenceNumber as SequenceNumber,
            s.DateStamp
    from Outbound835File o
        LEFT JOIN OutboundFinSegSequence s ON s.Id = o.OutboundFinSegSequenceId
    where o.Id = @Outbound835FileId

    select  p.Id
    from InboundPayment p
    join Outbound835FileTransaction t
        on p.Id = t.InboundPaymentId
    where t.Outbound835FileId = @Outbound835FileId

END