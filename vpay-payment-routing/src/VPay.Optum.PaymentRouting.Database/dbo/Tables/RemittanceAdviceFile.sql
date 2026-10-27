CREATE TABLE [dbo].[RemittanceAdviceFile]
(
    [Id]                    BIGINT          NOT NULL    IDENTITY (1, 1) ,
    [Payer835Id]            VARCHAR(10)     NOT NULL,   -- OptumPayerId: This is what's allocated in other place, even though I'm fairly certain it can only be 5 characters
    [UserKey]               VARCHAR(100)    NOT NULL,
    [InboundZipfile]        VARCHAR(512)    NOT NULL,
    [PayerAlias]            VARCHAR(5)      NOT NULL,     
    [OriginCode]            VARCHAR(50)     NOT NULL,   -- This is what iSeries NFPTPACFG table uses. Since that's the "lookup" from this table, keeping 50 characters
    [ClientCode]            VARCHAR(10)     NOT NULL,     
    [RemitMethod]           VARCHAR(10)     NOT NULL    CONSTRAINT [DF_RemittanceAdviceFile_RemitMethod]  DEFAULT ('PDF'),    

    [PdfExtractPath]        VARCHAR(512)    NOT NULL,
    [CreatedDateTime]       DATETIME2(7)    NOT NULL    CONSTRAINT [DF_RemittanceAdviceFile_CreatedDateTime] DEFAULT (sysdatetime()), 
    [CreatedBy]             VARCHAR(20)     NOT NULL, 

    [ExcludeRow]            BIT             NOT NULL    CONSTRAINT [DF_RemittanceAdviceFile_ExcludeRow] DEFAULT 0, 
    [JiraTicket]            VARCHAR(20)         NULL, 
    [BillingEntityCode]     VARCHAR(10)     NULL,
    CONSTRAINT [PK_RemittanceAdviceFile] PRIMARY KEY CLUSTERED ([Id] ASC),
)
GO
/* 
So, all "payments" must be unique within "Payer835Id/clientCode" and "UserKey/PaymentId", .  
However, they have indicated there is a chance we may get duplicates in separate Zip files
(no "updates" to existing payments will ever happen this way).  

The "default" behavior when that happens should  be to just insert the duplicate 
and overwrite the file on the filesystem.  This way, we can easily track duplicates for
reporting, but as long as all the Payer835Id/UserKeys match with rows in InboundPayment
the InboundBatch can be released.
*/
CREATE UNIQUE INDEX UNQ_RemittanceAdviceFile ON RemittanceAdviceFile (Payer835Id, InboundZipfile, UserKey )
    WITH (IGNORE_DUP_KEY = OFF);
GO

EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'If ExcludeRow is 1, this _should_ be populated with a JiraTicket Id to justify the decision to exclude row.',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'RemittanceAdviceFile',
    @level2type = N'COLUMN',
    @level2name = N'JiraTicket'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'Used by the aging report, for items that end up on aging report but will not be resolved by other means (getting the appropriate 835s, etc)',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'RemittanceAdviceFile',
    @level2type = N'COLUMN',
    @level2name = N'ExcludeRow'