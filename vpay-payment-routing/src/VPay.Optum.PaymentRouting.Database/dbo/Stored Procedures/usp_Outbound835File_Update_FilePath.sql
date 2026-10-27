CREATE PROCEDURE [dbo].[usp_Outbound835File_Update_FilePath]
    @Outbound835FileId BIGINT,
    @FilePath VARCHAR(256),
    @ModifiedBy VARCHAR(50)
AS
BEGIN

    UPDATE  dbo.Outbound835File
    SET     FilePath = @FilePath,
            LastModifiedDateTime = SYSDATETIME(),
            LastModifiedBy = @ModifiedBy
    WHERE   Id = @Outbound835FileId;

END