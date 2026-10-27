CREATE TABLE [dbo].[ApMessage] (
    [Id]                   BIGINT         IDENTITY (1, 1) NOT NULL,
    [TransactionId]        BIGINT         NOT NULL,
    [ClientCode]           VARCHAR (10)   NOT NULL,
    [BillingEntityCode]    VARCHAR (20)   NOT NULL,
    [ApMessageType]        VARCHAR (10)   NOT NULL,
    [ApRoutingType]        VARCHAR (10)   NOT NULL,
    [IsOptumEnrolled]      BIT            NOT NULL,

    -- Avro fields
    [PayerId]              VARCHAR (10)   NOT NULL,
    [SubPayerId]           VARCHAR (20)   NOT NULL,
    [PaymentSource]        VARCHAR (20)   NOT NULL,
    [PaymentType]          VARCHAR (10)   NOT NULL,
    [ProviderNumber]       VARCHAR (50)   NOT NULL,
    [ProviderName]         VARCHAR (100)  NOT NULL,
    [ProviderNpiNumber]        VARCHAR (20)   NOT NULL,
    [UnConsolPayNumber]    VARCHAR (100)  NOT NULL,
    [UnConsolPayAmount]    VARCHAR (50)   NOT NULL,
    [DisplayConsolPayNumber] VARCHAR (100)  NOT NULL,
    [ConsolAmount]         VARCHAR (50)   NOT NULL,
    [PayMethodCode]        VARCHAR (20)   NOT NULL,
    [SettlementDate]       VARCHAR (20)   NOT NULL,
    [CheckNumber]          VARCHAR (20)   NOT NULL,
    [ReturnReasonCode]     VARCHAR (20)   NOT NULL,
    [RevisedMethodCode]    VARCHAR (20)   NOT NULL,
    [ReOriginatedDate]     VARCHAR (20)   NOT NULL,

    -- Auditing
    [CreatedBy]            VARCHAR (50)   NOT NULL,
    [CreatedDateTime]      DATETIME2 (7)  CONSTRAINT [DF_ApMessage_CreatedDateTime] DEFAULT (SYSDATETIME()) NOT NULL,
    [ModifiedBy]           VARCHAR (50)   NULL,
    [ModifiedDateTime]     DATETIME2 (7)  CONSTRAINT [DF_ApMessage_ModifiedDateTime] DEFAULT (SYSDATETIME()) NULL,

    CONSTRAINT [PK_ApMessage] PRIMARY KEY NONCLUSTERED ([Id] ASC),
    CONSTRAINT [UNQ_ApMessage_TransactionId] UNIQUE CLUSTERED ([TransactionId] ASC, [ApMessageType] ASC)
);
GO