CREATE TABLE [dbo].[InboundPayment] (
    [Id]                   BIGINT          IDENTITY (1, 1) NOT NULL,
    [InboundBatchId]       BIGINT          NOT NULL,
    [ClobStorageId]        BIGINT          NOT NULL,
    [UserKey]              VARCHAR (100)   NOT NULL,
    [Payer835Id]           VARCHAR (5)     NOT NULL,
    [FileProcId]           VARCHAR (100)   NULL,
    [PayerAlias]           VARCHAR (20)    NOT NULL,
    [ProvTIN]              VARCHAR (10)    NOT NULL,
    [PayerSubpayerId]      VARCHAR (10)    NOT NULL,
    [ZipCode]              VARCHAR (10)    NOT NULL,
    [PaymentAmount]        DECIMAL (20, 2) NOT NULL,
    [CreatedDateTime]      DATETIME2 (7)   CONSTRAINT [df_InboundPayment_CreatedDateTime] DEFAULT (sysdatetime()) NOT NULL,
    [Status]               VARCHAR (20)    CONSTRAINT [DF_InboundPayment_Status] DEFAULT ('RECEIVED') NOT NULL,
    [OutboundRefEo]        VARCHAR (10)    NULL,
    [LastModifiedBy]       VARCHAR (50)    NULL,
    [LastModifiedDateTime] DATETIME2 (7)   CONSTRAINT [DF_InboundPayment_LastModifiedDateTime] DEFAULT (sysdatetime()) NOT NULL,
    [RemitMethod] VARCHAR(10)  CONSTRAINT [DF_InboundPayment_RemitMethod]  DEFAULT ('DATA') NULL, 
    CONSTRAINT [PK_InboundPayment] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [CHK_InboundPayment_Status] CHECK ([dbo].[udf_InboundPaymentStatus]([Status])=(1)),
    CONSTRAINT [FK_InboundPayment_ClobStorageId] FOREIGN KEY ([ClobStorageId]) REFERENCES [dbo].[ClobStorage] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_InboundPayment_InboundBatchId] FOREIGN KEY ([InboundBatchId]) REFERENCES [dbo].[InboundBatch] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [UNQ_InboundPayment_Payer835Id_UserKey] UNIQUE NONCLUSTERED ([Payer835Id] ASC, [UserKey] ASC)
);
GO

CREATE NONCLUSTERED INDEX [idx_InboundPayment-Status-INCL4]
    ON [dbo].[InboundPayment]([Status] ASC)
    INCLUDE([UserKey], [PayerAlias], [PaymentAmount], [OutboundRefEo]);
GO

CREATE NONCLUSTERED INDEX [idx_InboundPayment-InboundBatchId-INCL4]
    ON [dbo].[InboundPayment] ([InboundBatchId])
    INCLUDE ([UserKey],[Payer835Id],[PaymentAmount],[RemitMethod])
GO

CREATE TRIGGER [dbo].[TRG_InboundPayment_Status_Update]
    ON [dbo].InboundPayment
    AFTER INSERT, UPDATE
AS
BEGIN
    SET NoCount ON;
    INSERT  dbo.InboundPaymentStatusHistory (InboundPaymentId, [Status])
    SELECT  I.Id, I.[Status]
    FROM    Inserted I
END
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'Will need to be set when OUT835BUILT is set, and OutboundPaymentClob is inserted',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'InboundPayment',
    @level2type = N'COLUMN',
    @level2name = N'OutboundRefEo'