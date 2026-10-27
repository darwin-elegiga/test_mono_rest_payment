CREATE TABLE [dbo].[ApMessageTracking] (
    [Id]                   BIGINT          IDENTITY (1, 1) NOT NULL,
    [ApMessageId]          BIGINT          NOT NULL,
    [InboundPaymentId]     BIGINT          NULL,
    [TransactionId]        BIGINT          NOT NULL,
    [TrnSegment]           VARCHAR (256)   NOT NULL,
    [LoadAmount]           DECIMAL (20, 2) NOT NULL,
    [PaymentTypeCode]      VARCHAR (10)    NOT NULL,
    [CheckNumber]          VARCHAR (10)    NULL,
    [LoadDate]             DATETIME2 (7)   NOT NULL,
    [Status]               VARCHAR (20)    NOT NULL,
    [CreatedBy]            VARCHAR (50)    NOT NULL,
    [CreatedDateTime]      DATETIME2 (7)   CONSTRAINT [DF_ApMessageTracking_CreatedDateTime] DEFAULT (sysdatetime()) NOT NULL,
    [LastModifiedBy]       VARCHAR (50)    NOT NULL,
    [LastModifiedDateTime] DATETIME2 (7)   CONSTRAINT [DF_ApMessageTracking_LastModifiedDateTime] DEFAULT (sysdatetime()) NOT NULL,
    CONSTRAINT [PK_ApMessageTracking] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [CHK_ApMessageTracking_Status] CHECK ([dbo].[udf_ApMessageTrackingStatus]([Status])=(1)),
    CONSTRAINT [FK_ApMessageTracking_InboundPaymentId] FOREIGN KEY ([InboundPaymentId]) REFERENCES [dbo].[InboundPayment] ([Id]),
    CONSTRAINT [UNQ_ApMessageTracking_ApMessageId] UNIQUE NONCLUSTERED ([ApMessageId] ASC)
);
GO
CREATE TRIGGER [dbo].[TRG_ApMessageTracking_Status_Update]
    ON [dbo].ApMessageTracking
    AFTER INSERT, UPDATE
AS
BEGIN
    SET NoCount ON;
    INSERT  dbo.ApMessageTrackingStatus (ApMessageTrackingId, [Status],[CreatedBy])
    SELECT  I.Id, I.[Status], COALESCE(I.LastModifiedBy, I.CreatedBy)
    FROM    Inserted I
END

GO



GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'or AP = TransactionId, for REAP = ReapTransactionId',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'ApMessageTracking',
    @level2type = N'COLUMN',
    @level2name = N'ApMessageId'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'received TransactionId',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'ApMessageTracking',
    @level2type = N'COLUMN',
    @level2name = N'TransactionId'