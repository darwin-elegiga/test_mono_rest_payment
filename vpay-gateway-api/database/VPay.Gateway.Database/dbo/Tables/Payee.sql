CREATE TABLE dbo.Payee
(
    Id bigint NOT NULL IDENTITY (1, 1),
    OriginCode varchar(50) NOT NULL,
    ClientCode varchar(10) NOT NULL,
    MemberId varchar(20) NULL,
    [Name] varchar(100) NULL,
    Email varchar(100) NULL,
    Phone varchar(15) NULL,
    [CreatedDateTime] DATETIME2 NOT NULL CONSTRAINT [df_Payee_CreatedDateTime] DEFAULT (SYSDATETIME()),
    [LastModifiedDateTime] DATETIME2 NOT NULL CONSTRAINT [df_Payee_LastModifiedDateTime] DEFAULT (SYSDATETIME())
    CONSTRAINT [pk_Payee_Id] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
)  ON [PRIMARY]

GO

CREATE UNIQUE NONCLUSTERED INDEX ux_Payee_ClientCodeMemberId ON dbo.Payee
    (
        ClientCode,
        MemberId
    ) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]