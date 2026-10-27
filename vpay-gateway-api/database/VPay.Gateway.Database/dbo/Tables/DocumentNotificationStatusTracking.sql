CREATE TABLE [dbo].[DocumentNotificationStatusTracking]
(
    [Id] BIGINT NOT NULL PRIMARY KEY IDENTITY, 
    [DocumentNotificationId] BIGINT NOT NULL, 
    [Status] VARCHAR(64) NOT NULL, 
    [CreatedDateTime] DATETIME2 NOT NULL CONSTRAINT [df_DocumentNotificationStatusTracking_CreatedDateTime] DEFAULT (SYSDATETIME()), 
    CONSTRAINT [FK_DocumentNotificationStatusTracking_DocumentNotification] FOREIGN KEY (DocumentNotificationId) REFERENCES [DocumentNotification]([Id])
)
