CREATE PROCEDURE [dbo].[usp_Payee_Insert]
    @OriginCode varchar(50)
   ,@ClientCode varchar(10)
   ,@MemberId varchar(20)
   ,@Name varchar(100)
   ,@Email varchar(100)
   ,@Phone varchar(15)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[Payee] (
        [OriginCode]
       ,[ClientCode]
       ,[MemberId]
       ,[Name]
       ,[Email]
       ,[Phone]
    ) VALUES (
        @OriginCode
       ,@ClientCode
       ,@MemberId
       ,@Name
       ,@Email
       ,@Phone
   );

    EXEC [dbo].[usp_Payee_GetByClientAndMemberId] @ClientCode ,@MemberId

END
GO
