CREATE PROCEDURE [dbo].[usp_Outbound835File_UpdateStatus]
    @Id BIGINT,
    @Status VARCHAR(20),
    @UpdatedBy VARCHAR(50) = NULL,
    @UpdatedDateTime DATETIME2(7) = NULL,
    @SentDateTime DATETIME2(7) = NULL
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;

    BEGIN TRANSACTION
    BEGIN TRY
        DECLARE @Updated TABLE (
                [Id]					BIGINT           NOT NULL
            ,	[PayerAlias] 		    VARCHAR (10) 	 NOT NULL
            ,	[OutboundRefEO] 		VARCHAR (10) 	 NULL
            ,   [Status]                VARCHAR (20)     NOT NULL
            ,   [QueueMessageId]        UNIQUEIDENTIFIER NULL
            ,   [FilePath]              VARCHAR (256)    NULL
            ,   [CreatedBy]             VARCHAR (50)     NOT NULL
            ,   [CreatedDateTime]       DATETIME2 (7)    NOT NULL
            ,   [LastModifiedBy]        VARCHAR (50)     NOT NULL
            ,   [LastModifiedDateTime]  DATETIME2 (7)    NOT NULL
            ,   [SentDateTime]          DATETIME2 (7)    NULL
        );

        UPDATE
            [dbo].[Outbound835File]
        SET
                [Status] = @Status
            ,	[LastModifiedBy] = COALESCE(@UpdatedBy, suser_sname())
            ,	[LastModifiedDateTime] = COALESCE(@UpdatedDateTime, SYSDATETIME())
            ,	[SentDateTime] = COALESCE(@SentDateTime, [SentDateTime])
        OUTPUT
                INSERTED.[Id]
            ,	INSERTED.[PayerAlias]
            ,	INSERTED.[OutboundRefEO]
            ,	INSERTED.[Status]
            ,	INSERTED.[QueueMessageId]
            ,	INSERTED.[FilePath]
            ,	INSERTED.[CreatedBy]
            ,	INSERTED.[CreatedDateTime]
            ,	INSERTED.[LastModifiedBy]
            ,	INSERTED.[LastModifiedDateTime]
            ,	INSERTED.[SentDateTime]
        INTO
            @Updated
        WHERE
            [Id] = @Id

        SELECT  [Id]
            ,	[PayerAlias]
            ,	[OutboundRefEO]
            ,   [Status]
            ,   [QueueMessageId]
            ,   [FilePath]
            ,   [CreatedBy]
            ,   [CreatedDateTime]
            ,   [LastModifiedBy]
            ,   [LastModifiedDateTime]
            ,   [SentDateTime]
        FROM
            @Updated
        ;

        COMMIT TRANSACTION
        ;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
