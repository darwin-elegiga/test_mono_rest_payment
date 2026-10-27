CREATE PROCEDURE [dbo].[usp_PayeeStatus_GetByPayeeIdAndClaimIdAndStatusAndPartyStatus]
  @PayeeId bigint,
  @ClaimId varchar(32),
  @Status varchar(50),
  @PartyStatus varchar(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [Id]
       ,[PayeeId]
       ,[ClaimId]
       ,[PepPayeeId]
       ,[Status]
       ,[CurrentProcessStatus]
       ,[PepStatusId]
       ,[StatusDateTime]
       ,[CreatedDateTime]
       ,[LastModifiedDateTime]
       ,[PartyStatusId]
       ,[PartyStatus]
       ,[RetryCount]
    FROM
        [dbo].[PayeeStatus]
    WHERE
        [PayeeId] = @PayeeId
        AND [ClaimId] = @ClaimId
        AND [Status] = @Status
        AND COALESCE([PartyStatus], 'default value') = COALESCE(@PartyStatus, 'default value')  

END
GO
