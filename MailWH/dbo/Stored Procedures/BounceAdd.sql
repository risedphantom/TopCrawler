create procedure [dbo].[BounceAdd]
	@MessageID varchar(8000),
	@DateRecieved datetime,
	@AddressFrom varchar(8000),
    @AddressTo varchar(8000),
	@Subject varchar(8000),
    @ListId varchar(8000),
	@BounceTypeSysName varchar(200),
	@Message varchar(max),
	@RawMessage varbinary(max),
	@SourceFrom varchar(8000),
	@ID bigint out
as
begin
	insert	Bounce(MessageID, DateRecieved, AddressFrom, AddressTo, Subject, ListId, BounceTypeID, Message, RawMessage, SourceFrom)
	values (@MessageID,@DateRecieved,@AddressFrom,@AddressTo,@Subject,@ListId,@BounceTypeSysName,@Message,@RawMessage,@SourceFrom)

	set @ID = scope_identity()
end
