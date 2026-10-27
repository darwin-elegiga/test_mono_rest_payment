CREATE TABLE dbo.PayeeStatusTracking
(
    [Id] bigint NOT NULL IDENTITY (1, 1),
    [PayeeStatusId] bigint NOT NULL,
    [Status] varchar(128) NOT NULL,
    [CreatedDateTime] datetime2(7) NOT NULL CONSTRAINT [df_PayeeStatusTracking_CreatedDateTime] DEFAULT (SYSDATETIME())
    CONSTRAINT [pk_PayeeStatusTracking_Id] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
)  ON [PRIMARY]

GO

ALTER TABLE dbo.PayeeStatusTracking ADD CONSTRAINT
    fk_PayeeStatusTracking_PayeeStatus FOREIGN KEY
    (
        PayeeStatusId
    ) REFERENCES dbo.PayeeStatus
    (
        Id
    ) ON UPDATE  NO ACTION 
     ON DELETE  NO ACTION 