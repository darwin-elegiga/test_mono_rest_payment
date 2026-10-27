CREATE PROCEDURE [dbo].[usp_PayeeStatus_GetByClientAndMemberId]
  @ClientCode varchar(10),
  @MemberId varchar(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ps.[Id]
       ,ps.[PayeeId]
       ,ps.[ClaimId]
       ,ps.[PepPayeeId]
       ,ps.[Status]
       ,ps.[CurrentProcessStatus]
       ,ps.[PepStatusId]
       ,ps.[StatusDateTime]
       ,ps.[CreatedDateTime]
       ,ps.[LastModifiedDateTime]
       ,ps.[PartyStatusId]
       ,ps.[PartyStatus]
       ,ps.[RetryCount]
    FROM
        [dbo].[PayeeStatus] ps
    INNER JOIN [dbo].[Payee] p
        ON p.[Id] = ps.[PayeeId]
    WHERE
        p.[ClientCode] = @ClientCode
        AND p.[MemberId] = @MemberId
    ORDER BY
        ps.[StatusDateTime];

END
GO
