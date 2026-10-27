CREATE PROCEDURE [dbo].[usp_Payee_GetByClientAndMemberId]
  @ClientCode varchar(10),
  @MemberId varchar(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [Id]
       ,[OriginCode]
       ,[ClientCode]
       ,[MemberId]
       ,[Name]
       ,[Email]
       ,[Phone]
       ,[CreatedDateTime]
       ,[LastModifiedDateTime]
    FROM
        [dbo].[Payee]
    WHERE
        [ClientCode] = @ClientCode
        AND [MemberId] = @MemberId

END
GO
