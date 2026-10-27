CREATE TABLE [dbo].[PaymentStatusTracking]
(
    [Id] BIGINT NOT NULL IDENTITY(1,1), 
    [PaymentId] BIGINT NOT NULL, 
    [Status] VARCHAR(50) NOT NULL,
    [CreatedDateTime] datetime2(7) NOT NULL CONSTRAINT [df_PaymentStatusTracking_CreatedDateTime] DEFAULT (SYSDATETIME())
    CONSTRAINT [pk_PaymentStatusTracking_Id] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
)  ON [PRIMARY]

GO

ALTER TABLE dbo.PaymentStatusTracking ADD CONSTRAINT
    fk_PaymentStatusTracking_PayeeStatus FOREIGN KEY
    (
        PaymentId
    ) REFERENCES dbo.Payment
    (
        Id
    ) ON UPDATE  NO ACTION 
     ON DELETE  NO ACTION 