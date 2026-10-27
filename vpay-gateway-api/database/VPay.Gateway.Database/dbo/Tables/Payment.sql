CREATE TABLE [dbo].[Payment]
(
    [Id] BIGINT NOT NULL IDENTITY(1,1),
    [ClientCode] VARCHAR(10) NOT NULL,
    [ClientPaymentId] VARCHAR(100) NOT NULL,
    [LastSentStatus] VARCHAR(50) NOT NULL CONSTRAINT [df_Payment_LastSentStatus] DEFAULT (''),
    [CreatedDateTime] DATETIME2(7) NOT NULL CONSTRAINT [df_Payment_CreatedDateTime] DEFAULT (SYSDATETIME()),
    [LastModifiedDateTime] DATETIME2(7) NOT NULL CONSTRAINT [df_Payment_LastModifiedDateTime] DEFAULT (SYSDATETIME()),
    [CurrentBalance] DECIMAL(18,2) NOT NULL CONSTRAINT [df_Payment_CurrentBalance] DEFAULT (0)
    CONSTRAINT [pk_Payment_Id] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],    
    CONSTRAINT [uc_Payment_ClientCode_ClientPaymentId] UNIQUE ([ClientCode], [ClientPaymentId])
)  ON [PRIMARY]