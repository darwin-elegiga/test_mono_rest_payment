CREATE TABLE [dbo].[PaymentEventStatusTracking]
(
    [Id] BIGINT NOT NULL IDENTITY(1,1), 
    [PaymentEventId] BIGINT NOT NULL, 
    [Status] VARCHAR(50) NOT NULL, 
    [CreatedDateTime] DATETIME2(7) NOT NULL CONSTRAINT [df_PaymentEventStatusTracking_CreatedDateTime] DEFAULT (SYSDATETIME())
    CONSTRAINT [pk_PaymentEventStatusTracking_Id] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
)  ON [PRIMARY]
GO

ALTER TABLE dbo.PaymentEventStatusTracking ADD CONSTRAINT
    fk_PaymentEventStatusTracking_PaymentEvent FOREIGN KEY
    (
        PaymentEventId
    ) REFERENCES dbo.PaymentEvent
    (
        Id
    ) ON UPDATE  NO ACTION 
     ON DELETE  NO ACTION 