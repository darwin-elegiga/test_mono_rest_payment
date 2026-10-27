Create View dbo.vwRemittanceAdviceFileAgingReport
AS 
WITH cteRAF AS (
    SELECT  Payer835Id
            , PayerAlias
            , OriginCode
            , ClientCode
            , UserKey
            , InboundZipfile
            , PdfExtractPath
            , RemitMethod
            , CreatedDateTime
            , DateDiff(DAY, CreatedDateTime, SYSDATETIME())  AS TotalAge_Days
            , DateDiff(HOUR, CreatedDateTime, SYSDATETIME()) AS TotalAge_Hours
            , ROW_NUMBER()  OVER(Partition By Payer835Id, UserKey ORDER BY CreatedDateTime DESC) as RowNum
    FROM    dbo.RemittanceAdviceFile
    WHERE   ExcludeRow = 0
)
SELECT  R.Payer835Id
        , R.PayerAlias
        , R.OriginCode
        , R.ClientCode
        , R.UserKey
        , R.InboundZipfile
        , R.PdfExtractPath
        , R.RemitMethod
        , R.CreatedDateTime
        , R.TotalAge_Days
        , R.TotalAge_Hours
FROM    cteRAF AS R
        LEFT JOIN dbo.InboundPayment AS P
            ON R.Payer835Id=P.Payer835Id AND R.UserKey=P.UserKey AND P.RemitMethod=R.RemitMethod
WHERE   P.Id IS NULL AND
        RowNum = 1;