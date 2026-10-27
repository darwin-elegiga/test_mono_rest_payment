/*
    AUTH: judah burke
    PURP: append @JiraIssueId to [UserKey] for [Id] = @InboundPaymentId
    THROWS: 51001 if record does not exist for [Id] = @InboundPaymentId
    THROWS: 51002 if record does not exist for [Id] = @InboundPaymentId and [UserKey] = @UserKey
    THROWS: 51003 if record for [Id] = @InboundPaymentId has [Status] = 'BatchComplete'
    THROWS: 51004 if record for [Id] = @InboundPaymentId has [OutboundRefEo] IS NOT NULL
*/
CREATE PROCEDURE [dbo].[usp_InboundPayment_Update_UserKey]
    @InboundPaymentId BIGINT,
    @UserKey VARCHAR(49),
    @JiraIssueId VARCHAR(50)
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;

    BEGIN TRANSACTION
    BEGIN TRY
        DECLARE @ActualId BIGINT,
                @ActualUserKey VARCHAR(100),
                @ActualStatus VARCHAR(20),
                @ActualOutboundRefEo VARCHAR(10)
        SELECT  @ActualId = p.[Id], @ActualUserKey = p.[UserKey], 
                @ActualStatus = b.[Status], @ActualOutboundRefEo = p.[OutboundRefEo]
        FROM    [dbo].[InboundPayment] p
        LEFT JOIN [dbo].[InboundBatch] b ON b.[Id] = p.[InboundBatchId]
        WHERE   p.[Id] = @InboundPaymentId

        IF @ActualId IS NULL OR @ActualId <> @InboundPaymentId
            THROW 51001, 'The record does not exist for given @InboundPaymentId', 1
            
        IF @ActualUserKey IS NULL OR @ActualUserKey <> @UserKey
            THROW 51002, 'The record does not exist for given @UserKey', 1
            
        IF @ActualStatus = 'BatchComplete'
            THROW 51003, 'Cannot update [InboundPayment].[UserKey] if related [InboundBatch].[Status] is ''BatchComplete''', 1
            
        IF @ActualOutboundRefEo IS NOT NULL
            THROW 51004, 'Cannot update [InboundPayment].[UserKey] if [OutboundRefEo] is not null', 1

        UPDATE  [dbo].[InboundPayment]
        SET     [UserKey] = @UserKey + '_' + @JiraIssueId,
                [LastModifiedDateTime] = SYSDATETIME(),
                [LastModifiedBy] = @JiraIssueId
        WHERE   [Id] = @InboundPaymentId
            and [UserKey] = @UserKey;
                
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
