CREATE TABLE [dbo].[DocumentNotification]
(
    [Id] BIGINT NOT NULL PRIMARY KEY IDENTITY, 
    [TransactionId] BIGINT NOT NULL, 
    [ArchiveDetailId] BIGINT NOT NULL, 
    [ClientCode] VARCHAR(10) NOT NULL, 
    [BillingEntityCode] VARCHAR(20) NOT NULL, 
    [BillingEntityType] VARCHAR(20) NULL, 
    [CurrentStatus] VARCHAR(64) NOT NULL, 
    [CreatedDateTime] DATETIME2 NOT NULL CONSTRAINT [df_DocumentNotification_CreatedDateTime] DEFAULT (SYSDATETIME()), 
    [LastModifiedDateTime] DATETIME2 NOT NULL CONSTRAINT [df_DocumentNotification_LastModifiedDateTime] DEFAULT (SYSDATETIME()), 
    [PaymentId] VARCHAR(100) NOT NULL CONSTRAINT [df_DocumentNotification_PaymentId] DEFAULT '', 
    [DocumentDescription] VARCHAR(100) NOT NULL CONSTRAINT [df_DocumentNotification_DocumentDescription] DEFAULT '', 
    [DocumentType] VARCHAR(100) NOT NULL CONSTRAINT [df_DocumentNotification_DocumentType]  DEFAULT '', 
    [DocumentName] VARCHAR(256) NOT NULL CONSTRAINT [df_DocumentNotification_DocumentName]  DEFAULT '', 
    CONSTRAINT [ux_DocumentNotification_TransactionId_ArchiveDetailId] UNIQUE ([TransactionId], [ArchiveDetailId])
)
