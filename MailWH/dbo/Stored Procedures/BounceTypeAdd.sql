create procedure [dbo].[BounceTypeAdd]
	@SysName varchar(8000),
	@Name varchar(8000),
	@ID bigint out
as
begin
	insert	BounceType(SysName, Name)
	values	(@SysName, @Name)

	set @ID = scope_identity()
end
