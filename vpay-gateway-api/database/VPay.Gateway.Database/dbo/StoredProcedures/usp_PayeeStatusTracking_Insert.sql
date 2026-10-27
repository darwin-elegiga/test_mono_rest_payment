CREATE PROCEDURE [dbo].[usp_PayeeStatusTracking_Insert]
    @PayeeStatusId bigint
   ,@Status varchar(128)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[PayeeStatusTracking] (
        [PayeeStatusId]
       ,[Status]
    ) VALUES (
        @PayeeStatusId
       ,@Status
    );

    SELECT
        Id
       ,PayeeStatusId
       ,[Status]
       ,CreatedDateTime
    FROM
        [dbo].[PayeeStatusTracking]
    WHERE
        Id = SCOPE_IDENTITY();

END
GO
