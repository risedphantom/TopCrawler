CREATE TABLE [dbo].[FBL]
(
	[ID] BIGINT NOT NULL PRIMARY KEY IDENTITY, 
    [MessageID] VARCHAR(8000) NOT NULL, 
    [DateRecieved] DATETIME NOT NULL, 
    [AddressFrom] VARCHAR(8000) NULL, 
    [AddressTo] VARCHAR(8000) NOT NULL,
	[Subject] VARCHAR(8000) NULL,
	[FeedBackType] VARCHAR(200) NULL,
	[SourceIP] VARCHAR(200) NULL,
	[AuthResult] VARCHAR(8000) NULL,
	[OriginalMailFrom] VARCHAR(8000) NULL,
	[OriginalRcptTo] VARCHAR(8000) NULL,
	[OriginalSubject] VARCHAR(8000) NULL,
	[RawMessage] VARBINARY(max) NULL,
)
