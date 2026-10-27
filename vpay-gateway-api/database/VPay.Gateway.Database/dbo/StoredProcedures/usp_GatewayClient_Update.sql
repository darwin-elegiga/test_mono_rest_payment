CREATE PROCEDURE [dbo].[usp_GatewayClient_Update]
    @Id bigint,
    @ClientId varchar(100),
    @ClientSecret varchar(256),
    @OriginCode varchar(50),
    @ClientCode varchar(10),
    @ExpiredDateTime datetime2(7),
    @User varchar(50)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE 
        dbo.GatewayClient
    SET
        ClientId = @ClientId,
        ClientSecret = @ClientSecret,
        OriginCode = @OriginCode,
        ClientCode = @ClientCode,
        ExpiredDateTime = @ExpiredDateTime,
        LastModifiedBy = @User,
        LastModifiedDateTime = SYSDATETIME()
    WHERE
        Id = @Id

    exec [dbo].[usp_GatewayClient_GetById] @id

END
GO
