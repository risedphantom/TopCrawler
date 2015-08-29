CREATE TABLE [dbo].[Bounce]
(
	[ID] BIGINT NOT NULL PRIMARY KEY IDENTITY,
	[MessageID] VARCHAR(8000) NOT NULL, 
    [DateRecieved] DATETIME NOT NULL, 
    [AddressFrom] VARCHAR(8000) NULL, 
    [AddressTo] VARCHAR(8000) NOT NULL,
	[Subject] VARCHAR(8000) NULL,
	[ListId] VARCHAR(8000) NULL,
	[BounceTypeID] VARCHAR(200) NULL,
	[Message] VARCHAR(max) NULL,
	[RawMessage] VARBINARY(max) NULL, 
    [SourceFrom] VARCHAR(8000) NULL,
)

GO

CREATE INDEX [IX_Bounce_BounceTypeID_SourceFrom] ON [dbo].[Bounce] ([BounceTypeID],[SourceFrom])
