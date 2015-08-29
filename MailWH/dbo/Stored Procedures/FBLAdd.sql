create procedure [dbo].[FBLAdd]
	@MessageID varchar(8000),
	@DateRecieved datetime,
	@AddressFrom varchar(8000),
    @AddressTo varchar(8000),
	@Subject varchar(8000),
    @FeedBackType varchar(200),
	@SourceIP varchar(200),
	@AuthResult varchar(8000),
	@OriginalMailFrom varchar(8000),
	@OriginalRcptTo varchar(8000),
	@OriginalSubject varchar(8000),
	@RawMessage varbinary(max),
	@ID bigint out
as
begin
	insert	FBL (MessageID, DateRecieved, AddressFrom, AddressTo, Subject, FeedBackType, SourceIP, AuthResult, OriginalMailFrom, OriginalRcptTo, OriginalSubject, RawMessage)
	values (@MessageID,@DateRecieved,@AddressFrom,@AddressTo,@Subject,@FeedBackType,@SourceIP,@AuthResult,@OriginalMailFrom,@OriginalRcptTo,@OriginalSubject,@RawMessage)

	set @ID = scope_identity()
end
