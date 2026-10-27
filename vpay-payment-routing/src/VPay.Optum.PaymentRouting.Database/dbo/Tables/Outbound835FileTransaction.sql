CREATE TABLE [dbo].Outbound835FileTransaction
(
    Id                  BIGINT          NOT NULL    IDENTITY (1, 1)
                                                    CONSTRAINT PK_Outbound835FileTransaction PRIMARY KEY CLUSTERED (Id ASC)
,   InboundPaymentId    BIGINT          NOT NULL    CONSTRAINT FK_Outbound835FileTransaction_InboundPaymentId  FOREIGN KEY (InboundPaymentId)  REFERENCES [dbo].InboundPayment (Id)
,   Outbound835FileId   BIGINT          NOT NULL    CONSTRAINT FK_Outbound835FileTransaction_Outbound835FileId FOREIGN KEY (Outbound835FileId) REFERENCES [dbo].Outbound835File (Id)
,   CreatedDateTime     DATETIME2(7)    NOT NULL    CONSTRAINT DF_Outbound835FileTransaction_CreatedDateTime DEFAULT(SYSDATETIME())

)
GO

CREATE NONCLUSTERED INDEX [idx_Outbound835FileTransaction-Outbound835FileId-INCL1]
    ON [dbo].[Outbound835FileTransaction]([Outbound835FileId] ASC)
    INCLUDE([InboundPaymentId]);
GO

