CREATE PROCEDURE [dbo].[usp_RemittanceAdviceFile_Insert]
 @Payer835Id            VARCHAR(10),
 @InboundZipfile        VARCHAR(512),
 @UserKey               VARCHAR(100),
 @PayerAlias            VARCHAR(5),
 @OriginCode            VARCHAR(50),
 @ClientCode            VARCHAR(10),
 @BillingEntityCode     VARCHAR(10),
 @RemitMethod           VARCHAR(10),
 @PdfExtractPath        VARCHAR(512),
 @CreatedBy             VARCHAR(20)  
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @RemittanceAdviceFileId BIGINT;
    INSERT dbo.RemittanceAdviceFile ([Payer835Id], [InboundZipfile], [UserKey], [PayerAlias], [OriginCode], [ClientCode], [BillingEntityCode], [RemitMethod], [PdfExtractPath], [CreatedDateTime], [CreatedBy])
    SELECT @Payer835Id,
           @InboundZipfile,
           @UserKey,
           @PayerAlias,
           @OriginCode,
           @ClientCode,
           @BillingEntityCode,
           @RemitMethod,
           @PdfExtractPath,
           SYSDATETIME(),
           @CreatedBy;

    SET @RemittanceAdviceFileId = SCOPE_IDENTITY();
    -- If there are NO payments for this Pdf, P.InboundBatchId will be NULL
    SELECT      R.Id AS RemittanceAdviceFileId, P.InboundBatchId
    FROM        dbo.RemittanceAdviceFile R
                LEFT JOIN dbo.InboundPayment AS P
                    ON P.UserKey=R.UserKey AND
                    P.Payer835Id=R.Payer835Id
    WHERE       R.Id=@RemittanceAdviceFileId;
END
GO
