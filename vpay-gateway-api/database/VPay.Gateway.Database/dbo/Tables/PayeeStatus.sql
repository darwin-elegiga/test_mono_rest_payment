CREATE TABLE dbo.PayeeStatus
(
    Id bigint NOT NULL IDENTITY (1, 1),
    PayeeId bigint NOT NULL,
    ClaimId varchar(32) NOT NULL,
    PepPayeeId bigint NOT NULL,
    [Status] varchar(50) NOT NULL,
    [CurrentProcessStatus] varchar(50) NOT NULL,
    PepStatusId bigint NOT NULL,
    [StatusDateTime] varchar(50) NOT NULL,
    [CreatedDateTime] DATETIME2 NOT NULL CONSTRAINT [df_PayeeStatus_CreatedDateTime] DEFAULT (SYSDATETIME()),
    [LastModifiedDateTime] DATETIME2 NOT NULL CONSTRAINT [df_PayeeStatus_LastModifiedDateTime] DEFAULT (SYSDATETIME()),
    PartyStatusId bigint NULL,
    PartyStatus varchar(50) NULL,
    RetryCount int NULL CONSTRAINT [df_PayeeStatus_RetryCount] DEFAULT ((0)),
    CONSTRAINT [pk_PayeeStatus_Id] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY], 
)  ON [PRIMARY]

GO

ALTER TABLE dbo.PayeeStatus ADD CONSTRAINT
    fk_PayeeStatus_Payee FOREIGN KEY
    (
        PayeeId
    ) REFERENCES dbo.Payee
    (
        Id
    ) ON UPDATE  NO ACTION 
     ON DELETE  NO ACTION 
GO

CREATE UNIQUE NONCLUSTERED INDEX [ux_PayeeStatus_PayeeId_ClaimId_Status_PartyStatus]
    ON [dbo].[PayeeStatus] ([PayeeId], [ClaimId], [Status], [PartyStatus])
