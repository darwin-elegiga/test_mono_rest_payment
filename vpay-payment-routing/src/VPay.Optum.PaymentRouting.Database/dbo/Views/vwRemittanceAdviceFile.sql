Create  View dbo.vwRemittanceAdviceFile
AS 
WITH cteRAF AS (
SELECT [Payer835Id]
      ,[PayerAlias]
      ,[OriginCode]
      ,[ClientCode]
      ,[UserKey]
      ,[RemitMethod]
      ,[PdfExtractPath]
	  ,CreatedDateTime
	  , ROW_NUMBER()  OVER(Partition By [Payer835Id], [UserKey] ORDER BY CreatedDateTime DESC) as RowNum
  FROM dbo.RemittanceAdviceFile
)
SELECT [Payer835Id]
      ,[PayerAlias]
      ,[OriginCode]
      ,[ClientCode]
      ,[UserKey]
      ,[RemitMethod]
      ,[PdfExtractPath]
	  ,CreatedDateTime
FROM cteRAF 
WHERE RowNum = 1